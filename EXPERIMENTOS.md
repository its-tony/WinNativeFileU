# Bitácora
Sesión: 2026-10-18 America/Guatemala (UTC-06:00). Los archivos originales están en evidence/private, excluidos de Git.

## EXP-R0-01 — Workspace y entorno
Objetivo: identificar el destino y herramientas sin alterar el laboratorio padre.
Procedimientos: Get-ChildItem -Force; git status --short; git log -3 --oneline; git remote -v; dotnet --info; node --version; python --version; java -version; Get-Command de herramientas solicitadas; lectura del registro Windows y SDK bin.
Resultado: repositorio inicial c8e2451 limpio con LICENSE/.gitattributes; remoto ya existente, sin operación remota. Windows registro build 26200/25H2 (ProductName legado indica Windows 10 Home); .NET host 10.0.13 sin SDK, Node25.8.1, Python3.12.6, Java21.0.7. Windows SDK bin hasta22000 observado, sin inferir instalación completa.
Evidencia: evidence/private/initial-environment.json y transcripción de herramientas para inventario complementario.
Limitación: orden combinada salida0 no significa que WSL funcionó: wsl --list --quiet reportó E_ACCESSDENIED. No se atribuye a ausencia de WSL.

## EXP-R0-02 — WSL y REA
REA binary_session({}) respondió, sin target abierto:6.4.0,170tools, Ghidra 12.1.4/JDK21.0.12.1 disponibles según diagnósticos. No decompilación.
Primer comando WSL fuera sandbox usó sh -lc y falló1 por perfil swiftly/env.sh (Bad substitution). Se cambió a bash --noprofile --norc, evidencia nueva de causa de perfil.
Procedimiento válido: wsl -d Ubuntu --exec bash --noprofile --norc -c con uname -sr, command -v de herramientas, sha256sum de LICENSE compartida y ayuda del wrapper REA instalado. Ruta personal del wrapper omitida en resumen público; comando exacto en transcripción y evidence/private/wsl-procedure.json.
Salida0: Linux6.6.87.2 WSL2; python3/java/file/readelf/strings/7z en PATH, Node/llvm-objdump/llvm-otool no localizados en ese PATH. REA wrapper responde ayuda. Hash LICENSE coincide con Windows: bc83f561c857090f9cf0b8aca33afe369b72986ce13095dbebe4f5e751135886. Un solo checkout compartido.
Evidencia: evidence/private/rea-session.json; transcripción para WSL/hashes. No reinstalación ni configuración global.
Siguiente: sin binario legítimo de Tangerine no procede Ghidra.

## EXP-R0-03 — Fuentes y demo pública
Herramientas: web lector y navegador IAB; versiones del servicio no expuestas.
Procedimiento: abrir sitio oficial, press kit y ficha Apple enlazada. En demo, landscape.jpg -> Return -> Right -> Escape.
Resultado OBSERVED (web simulada): aparece menú PNG/WebP/HEIC/TIFF/AVIF/BMP/PDF; Right cambia indicación a WebP; Escape quita menú y devuelve foco al archivo. No se confirmó operación ni se cargaron archivos del usuario. Código no aplica.
Evidencia: evidence/private/web-observation.json; docs/research/tangerine/sources.md. La web declara simulación; no se observó Finder ni arrastre nativo.
Distribución pública remite a Mac App Store; no paquete directo verificado. No se intentó evadir cuenta/compra/acceso.
Siguiente: especificación independiente y evaluación Windows.

Entrega R0: siete documentos en docs/research/tangerine, especificación independiente y dos ADRs. Tarea R0 resuelta con alcance de investigación pública; no desbloquea análisis nativo ni acredita Windows prototype. Inspección visual adicional de la simulación mostró siete sectores alrededor de un centro, selección naranja y etiquetas contrastantes; sin copiar recursos ni medir animación/latencia. Screenshot solo en transcripción de herramienta, no archivo público.

## EXP-M0-01 — SDK local
Primer fetch metadata en sandbox falló1: host desconocido. Mismo origen fuera sandbox funcionó0: metadata oficial declara SDK 10.0.402. Cambio de permisos resuelve DNS sin cambiar validación TLS.
Instalación exacta y resultado final se registran mediante tracker SDK en evidence/private/sdk-install-01.json; SHA512/origen en sdk-provenance.json al completarse. No instalar globalmente ni suponer compilación por presencia del runtime.

Resultado final SDK: salida0, SDK 10.0.402/MSBuild18.9.15; SHA512 coincidente con metadata Microsoft. Solo .tools local. Tarea SDK VERIFICADO por evidencia de instalación real.

## EXP-M0-02 — Primera compilación
Comando: python scripts/task_tracker.py run BUILD --evidence evidence/private/build-01.json --note 'Restore/build Windows WinUI shell and run domain tests; review individual results' --timeout 300 --command pwsh -NoProfile -File scripts/build.ps1
Resultado: salida1, MSB4126 Debug|x64 no declarado en solución slnx; no compilación ni tests ejecutados. Causa identificada: faltaba Configurations/Platform x64. Se añadió esa plataforma explícita antes del siguiente intento; no se repite sin cambio.
Evidencia: evidence/private/build-01.json. Siguiente: restaurar/compilar con configuración corregida.

## EXP-M0-03 — Compilación Windows válida, invocación de tests incorrecta
Comando: mismo tracker BUILD, evidence/private/build-02.json, script/build.ps1 corregido en x64 de solución.
Resultado: restore0 y compilación Windows real correcta, 0 advertencias/0 errores; App/Core/Tests producidos. Orden final test falló1: solicitó bin/x64/Release cuando solución había compilado Core/Tests en bin/Release (AnyCPU). Ninguna prueba ejecutada en este intento. No se confunde fallo del runner con fallo de compilación.
Causa nueva: la invocación directa forzaba Platform=x64 mientras slnx mapea proyectos de dominio a AnyCPU. Se quitó ese parámetro de dotnet test. Tracker conservador cerró BUILD tras dos fallos de comando, de causas distintas. Reapertura explícita con scripts/build.ps1 modificado como evidencia nueva; no repetir sin corrección. Siguiente: build con locked restore y tests corregidos.

## EXP-M0-REVIEW-01 — Revisión de rutas
Comando tracker REVIEW ejecuta python scripts/review-repository.py; salida0. Revisa rutas candidatas, siete documentos requeridos y git diff --check. Original evidence/private/review-01.json. Alcance: no escáner universal de secretos ni sustituto de revisión manual. Estado/documentación pendiente de revisión final del contenido.

## EXP-M0-04 — Verificación final de build y dominio
Comando: python scripts/task_tracker.py run BUILD --evidence evidence/private/build-03.json --note 'Locked restore and corrected AnyCPU test invocation after documented path mismatch' --timeout 180 --command pwsh -NoProfile -File scripts/build.ps1 -LockedRestore.
Resultado: salida0; locked restore correcto; App/Core/Tests compilados Windows x64/AnyCPU según proyecto;0 advertencias/0 errores. TRX:4 totales,4 ejecutadas,4 aprobadas,0 fallidas/omitidas. Modelo probado: activación no entrega archivos, reset rechaza drop tardío en misma sesión, snapshot independiente/inmutable y entrada inválida no confirma selección. No se hicieron operaciones de archivo ni se probó concurrencia/adaptador OLE.
Evidencia: evidence/private/build-03.json; original de TRX en artifacts/test-results/core.trx copiado a evidence/private/core-tests-03.trx. Tareas BUILD y TEST VERIFICADO en alcance M0. No es VERIFIED IN WINDOWS PROTOTYPE para el gesto: ni ventana ejecutada manualmente, ni Explorer/hotkey/drop probados.
Siguiente: revisión de contenidos y commits locales; no avanzar a M1.

## EXP-M0-REVIEW-02 — Revisión final y Git local
Fecha:2026-10-18 America/Guatemala. Revisión manual de fuente C#/XAML/scripts, docs y lock files; script de rutas con documentos finales salida0 en evidence/private/review-02.json. Patrón acotado de claves API/GitHub/llaves privadas:0coincidencias en candidatos; no garantía universal. Se retiró ruta personal WSL del resumen público y se preservó procedimiento derivado de transcripción en privado. git check-ignore confirma SDK/evidencia/bin/resultados excluidos; LICENSE y .gitattributes sin diff.
Commit local de investigación e519edd,11archivos revisados; no binarios propietarios ni originales privados. Fundación se prepara aparte. Sin push, release ni modificación del remoto. Tarea REVIEW VERIFICADO para revisión local; CI/remoto y pruebas manuales siguen pendientes.

## EXP-M1-01 — Viabilidad y primera compilación
Fecha: 2026-10-18 America/Guatemala. Propietario autoriza M1 y prohíbe M2. Revisión de contratos primarios en ADR0003; sin nueva investigación nativa Tangerine. APIs públicas permiten receptor OLE y hotkeys por separado; combinación durante drag sigue hipótesis.
Prototipo HWND propio dentro host WinUI STA, CF_HDROP, whitelist de fixtures, COPY/NONE, F8 no-repeat/F9 rechazo, sin hooks. Conservar originales; solo se leen atributos/rutas. Diario privado con callbacks. No API de selección interna de Explorer.
Comando exacto en evidence/private/m1-build-01.json, tracker M1_BUILD ejecuta pwsh -NoProfile -File scripts/build.ps1. Salida0,0 advertencias/errores,16/16tests. Tests cubren política de efectos, límites léxicos de paths y estados; no COM/OLE real. TRX preservado evidence/private/m1-core-tests-01.trx.
Se añadieron después primer DragOver logueado y botón de fixtures: build-02 con -LockedRestore en curso, evidencia privada separada. Siguiente: ejecución real Windows antes de promover H-OLE.

## EXP-M1-02 — Primera ventana real y fallo de diagnóstico
build-02 terminó0,0 advertencias/errores y16/16tests. Lanzamiento mediante tracker M1_DESKTOP -> start-prototype.ps1 salida0 y fixtures nuevos con manifiesto/hash; no equivale a inicialización correcta.
Ventana real observada/activada con computer-use: botones deshabilitados y error Serialization of System.IntPtr not supported, Path $.details.hwnd. Diario solo OleInitialize(hr0). Screenshot privado m1-startup-failure-01.jpg. No recepción de archivos ni hotkey disponible en este intento.
Causa confirmada visualmente: JSON de handle nativo. Se convirtió a Int64 y se asignó ownership del registro antes del log para garantizar Revoke en cleanup. Se añadieron hitos de creación para diagnóstico. M1_DESKTOP registra FALLO1. Ventana cerrada normalmente; no terminar procesos ajenos. build-03 con locked restore verifica corrección; M1_BUILD reabierto explícitamente con fuente modificada, sin repetir ciegamente.

## EXP-M1-03 — Registro nativo y activación real desde Explorer
build-03 -LockedRestore salida0,0 advertencias/errores,16/16tests. TRX preservado m1-core-tests-03.trx. Launcher02 creó otro run/fixtures/manifiesto sin sobrescribir anteriores. Diario Ready: STA, OleInitialize hr0, RegisterDragDrop hr0, F8/F9 true. Ventana real muestra botones habilitados. Estos son resultados de ejecución Windows, no solo documentación.
Con computer-use se abrió el directorio nuevo mediante botón Open fixtures. Explorer mostró exclusivamente los dos fixtures creados. F8 desde esa ventana mostró rectángulo receptor; diario Armed registró foreground antes=después y DPI144 (150 %),540x240px. No se registran esos handles en resumen público. Snapshot privado m1-ready-armed-snapshot.jsonl.
Intento sky.drag desde fixture-one.txt hasta rectángulo fue rechazado antes de inyectarse: punto de destino sobre WinNativeFileU, no ventana target Explorer. Cero callbacks DragEnter/Drop en ese intento. No repetir cambiando solo activación, que no cambia ownership entre procesos. Evidencia privada m1-automation-boundary-01.json deriva del resultado de herramienta; no es fallo de viabilidad OLE.
check-prototype-files.ps1 -RunFile del segundo run salió0: nombres/tamaños/SHA256 intactos. No recepción ni cancelación real desde Explorer demostradas aún. Se solicitó al propietario prueba manual de F8 durante drag, selección múltiple Unicode y Escape; pendiente. La API de automatización tampoco documenta keypress intercalado con mouse hold.

## EXP-M1-04 — Recepción real de un archivo Unicode
Propietario informó: arrastró un archivo al cuadro blanco y desapareció. Procedimiento humano, no sky.drag. Diario correlacionado: DragEnter permitido7/devueltoCOPY1/count1, DragOver COPY1, DropAccepted COPY1/count1/niño-dos.txt y SessionEnded DropFinished. Fuente visible preparada en Explorer. Snapshot privado m1-single-drop-snapshot.jsonl; reporte propietario m1-operator-01.json. No conversión/copia implementada; COPY es negociación no destructiva.
Después del gesto, check-prototype-files.ps1 salida0: ambos nombres/tamaños/SHA256 coinciden con baseline. Alcance VERIFIED IN WINDOWS PROTOTYPE: recepción OLE desde Explorer de fixture Unicode en destino previamente armado. Armed precedió por más de un minuto a DragEnter: este caso NO prueba tecla durante drag. Nueva solicitud al propietario para selección múltiple con F8 durante drag y Escape; pendiente.

## EXP-M1-05 — Segundo intento inconcluso y límite de automatización
Propietario reportó «no esta funcionando, prueba tu igual». Diario posterior al primer Drop: Armed, luego Enter/Over count1, Leave y SessionEnded LeaveOrSourceCancel; no segundo DropAccepted ni selección múltiple observada. No atribuirlo a Escape ni a un defecto específico sin evidencia adicional. Manifiesto revalidado salida0: ambos fixtures intactos.
Se intentó inspeccionar ventanas para investigar directamente. computer-use notificó detención por Escape físico del usuario; no se emitieron más acciones de escritorio ni se sustituyó por otra vía de automatización. Registro de interrupción privado m1-interruption-01.json y snapshot final m1-final-desktop-snapshot.jsonl. El bloqueo previo cross-process de sky.drag se conserva; no se repitió un intento equivalente.
M1_DESKTOP BLOQUEADO para criterio completo, con logros parciales preservados. No se declara M1 terminado ni se avanza a M2. Próximo ensayo requiere una nueva sesión de escritorio autorizada y pruebas instrumentadas de F8 durante drag/múltiples/Escape. El prototipo queda abierto para cierre normal del propietario; liberación completa al cerrar no observada en la ejecución corregida, no afirmarla como probada.

## EXP-M1-REVIEW — Entrega parcial revisada
Fuente/interop/UI/scripts y documentación revisados; git diff --check y scripts/review-repository.py salida0. Evidencia privada m1-review-01.json, resumen público docs/verification/m1.json con hashes de snapshots y16tests. Ningún binario, screenshot, fixture o original privado candidato a Git. No garantía universal de escaneo de secretos.
Se reconcilió R0 heredado EN_PROGRESO con reporte/commits ya entregados, sin repetir investigación. El tracker modifica JSON sin lock; futuras mutaciones deben ser secuenciales para evitar pérdida de estados mientras un run sigue activo. Se documentó además copiar TRX antes de siguiente ejecución y tomar snapshots de diarios vivos. No se alteraron resultados históricos ni restricciones de fallos.
M1_REVIEW VERIFICADO para entrega local. M1_DESKTOP permanece BLOQUEADO con alcance incompleto; sin M2, push ni release. Commits previos c126b91 y5ec8c85; reporte de validación se conserva en commit separado.
