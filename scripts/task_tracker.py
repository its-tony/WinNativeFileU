"""Registro acotado: dos fallos bloquean una tarea; reapertura exige evidencia nueva."""
import argparse
import hashlib
import json
from datetime import datetime, timezone, timedelta
from pathlib import Path
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
STATES = {'PENDIENTE', 'EN_PROGRESO', 'VERIFICADO', 'DESCARTADO', 'BLOQUEADO'}

def now():
    return datetime.now(timezone(timedelta(hours=-6))).isoformat()

def save(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_suffix('.tmp')
    tmp.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    tmp.replace(path)

def digest(path):
    if not path.is_file():
        raise ValueError('La evidencia debe ser un archivo existente')
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def find(data, ident):
    return next(t for t in data['tasks'] if t['id'] == ident)

def check(task):
    if task['state'] in {'BLOQUEADO', 'DESCARTADO', 'VERIFICADO'} or task['equivalent_failures'] >= 2:
        raise ValueError('Estrategia cerrada: reapertura explícita con evidencia nueva requerida')

def record(data, task, outcome, evidence, note, count_attempt=True):
    check(task)
    sha = digest(evidence)
    if count_attempt:
        task['attempts'] += 1
    task['evidence'].append(str(evidence))
    if outcome == 'FALLO':
        task['equivalent_failures'] += 1
        task['state'] = 'BLOQUEADO' if task['equivalent_failures'] >= 2 else 'EN_PROGRESO'
    else:
        task['state'] = outcome
    task['next_step'] = note
    data['history'].append({'at': now(), 'id': task['id'], 'event': outcome,
                            'attempt': task['attempts'], 'evidence': str(evidence), 'sha256': sha, 'note': note})

def reopen(data, task, evidence, reason):
    sha = digest(evidence)
    if task['state'] not in {'BLOQUEADO', 'DESCARTADO', 'VERIFICADO'}:
        raise ValueError('La tarea no está cerrada')
    if any(h.get('sha256') == sha for h in data['history'] if h.get('id') == task['id']):
        raise ValueError('La evidencia ya fue usada; no justifica reapertura')
    if not reason.strip():
        raise ValueError('Explicar cambio de hipótesis o condición')
    task['state'] = 'PENDIENTE'
    task['equivalent_failures'] = 0
    task['next_step'] = reason
    data['history'].append({'at': now(), 'id': task['id'], 'event': 'REAPERTURA',
                            'evidence': str(evidence), 'sha256': sha, 'note': reason})

def self_test():
    base = ROOT / 'evidence/private/tracker-tests'
    base.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(dir=base) as directory:
        d = Path(directory)
        evidence = d / 'e.txt'
        evidence.write_text('fallo reproducido', encoding='utf-8')
        task = {'id':'TEST','state':'PENDIENTE','attempts':0,'equivalent_failures':0,'evidence':[]}
        data = {'tasks':[task], 'history':[{'event':'USUARIO', 'note':'Evento global sin ID de tarea'}]}
        record(data, task, 'FALLO', evidence, 'investigar causa')
        record(data, task, 'FALLO', evidence, 'cambiar método')
        assert task['state'] == 'BLOQUEADO'
        try:
            record(data, task, 'VERIFICADO', evidence, 'no permitido')
            raise AssertionError('tercer intento admitido')
        except ValueError:
            pass
        try:
            reopen(data, task, evidence, 'mismo archivo')
            raise AssertionError('reapertura sin novedad')
        except ValueError:
            pass
        fresh = d / 'nuevo.txt'
        fresh.write_text('dependencia reparada', encoding='utf-8')
        reopen(data, task, fresh, 'dependencia reparada; hipótesis distinta')
        record(data, task, 'VERIFICADO', fresh, 'continuar siguiente tarea')
        assert task['attempts'] == 3 and task['state'] == 'VERIFICADO'
        save(d / 'tasks.json', data)
        assert json.loads((d / 'tasks.json').read_text(encoding='utf-8')) == data
    print('PASS: dos fallos bloquean, tercer intento rechazado, evidencia repetida rechazada, reapertura y persistencia verificadas')

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--file', type=Path, default=ROOT / 'docs/tareas.json')
    sub = p.add_subparsers(dest='action', required=True)
    sub.add_parser('list')
    sub.add_parser('self-test')
    for name in ('check', 'record', 'resolve', 'reopen', 'run'):
        c = sub.add_parser(name)
        c.add_argument('id')
        if name in ('record', 'resolve', 'reopen', 'run'):
            c.add_argument('--evidence', type=Path, required=True)
            c.add_argument('--note', required=True)
        if name in ('record', 'resolve'):
            c.add_argument('--outcome', choices=sorted(STATES | {'FALLO'}), required=True)
        if name == 'run':
            c.add_argument('--timeout', type=float, default=30)
            c.add_argument('--command', nargs=argparse.REMAINDER, required=True)
    a = p.parse_args()
    if a.action == 'self-test':
        self_test()
        return 0
    data = json.loads(a.file.read_text(encoding='utf-8'))
    if a.action == 'list':
        for t in data['tasks']:
            print(t['id'], t['state'], f"intentos={t['attempts']}", t['next_step'])
        return 0
    task = find(data, a.id)
    if a.action == 'check':
        check(task)
        print('Permitido: criterio de tarea aún abierto')
        return 0
    if a.action == 'reopen':
        reopen(data, task, a.evidence, a.note)
    elif a.action == 'record':
        record(data, task, a.outcome, a.evidence, a.note)
    elif a.action == 'resolve':
        if a.outcome == 'FALLO':
            raise ValueError('Los fallos son intentos: usar record')
        record(data, task, a.outcome, a.evidence, a.note, count_attempt=False)
    elif a.action == 'run':
        check(task)
        command = a.command[1:] if a.command[:1] == ['--'] else a.command
        if not command or not 0 < a.timeout <= 300:
            raise ValueError('Comando obligatorio; timeout entre 0 y 300 segundos')
        if a.evidence.exists():
            raise ValueError('No sobrescribir evidencia existente')
        # Guardar la reserva antes del proceso: una interrupción no permite repetir silenciosamente.
        task['attempts'] += 1
        task['state'] = 'BLOQUEADO'
        data['history'].append({'at': now(), 'id': a.id, 'event':'RESERVA', 'note':'Proceso en curso; si se interrumpe, revisar evidencia y reabrir explícitamente'})
        save(a.file, data)
        started = now()
        try:
            result = subprocess.run(command, capture_output=True, text=True, errors='replace', timeout=a.timeout, shell=False)
            code, out, err = result.returncode, result.stdout, result.stderr
        except subprocess.TimeoutExpired:
            code, out, err = 124, '', 'Tiempo agotado; salida parcial omitida'
        except OSError as e:
            code, out, err = 127, '', type(e).__name__
        save(a.evidence, {'at':started,'command':command,'timeout':a.timeout,'exit_code':code,'stdout':out,'stderr':err})
        task['state'] = 'EN_PROGRESO'
        record(data, task, 'EN_PROGRESO' if code == 0 else 'FALLO', a.evidence, a.note, count_attempt=False)
        save(a.file, data)
        print(f'Código={code}; estado={task["state"]}; revisar evidencia antes de marcar VERIFICADO')
        return code
    save(a.file, data)
    print(task['id'], task['state'], f"intentos={task['attempts']}")
    return 0

if __name__ == '__main__':
    try:
        sys.exit(main())
    except (ValueError, KeyError, StopIteration, OSError) as exc:
        print('ERROR:', str(exc), file=sys.stderr)
        sys.exit(2)
