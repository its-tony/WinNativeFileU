# Estado — WinNativeFileU
Actualizado: 2026-10-18, America/Guatemala (UTC-06:00).

**M1 parcial. No avanzar a M2.** El propietario autorizó continuar M1; R0/M0 se conservan en sus informes y commits.

- Implementado y conservado en bc5fd96: reentrada tras Leave con botón observado abajo, diagnósticos F8/F9/ArmIgnored, estado accesible y texto correcto tras F9. No hooks, polling ni conversores.
- Última compilación real Windows: m1-native-build-03.json, locked restore, salida0,0 advertencias/errores.16Core+10tests nativos aprobados. Los10 usan HWND/STA/Register/Revoke/HGLOBAL reales, IDataObject sintético y callbacks directos; no DoDragDrop/Explorer ni entrada inyectada.
- Recepción real ampliada: dos ensayos desde Explorer recibieron ambos fixtures, incluido Unicode, con efecto COPY1. Propietario confirmó activación con botón Arm target antes del arrastre; diario registra Armed con botón suelto y sin nuevo HotkeyReceived. Nombres/tamaños/SHA256 siguen intactos. La recepción individual anterior y RevokeDragDrop0 se conservan como evidencia histórica.
- UI reanudada verificada: F8/Armed, F8 repetido/ArmIgnored y F9/CancelIdle. Texto final observado: Receiver closed. No file operation was performed. Pointer.LeftButtonDown=false; no prueba de activación durante drag. DPI observado144.
- M1_BATCH VERIFICADO para receptor prearmado. M1_DESKTOP BLOQUEADO para aceptación completa: sky.drag cruzado ya fue rechazado; no se repite. Snapshot nuevo m1-batch-drop-snapshot-01.jsonl conserva los dos DropAccepted. El cierre final de esta ejecución no se observó todavía.
- F8/key-up durante drag, reentrada física, Escape/F9 con final del origen, abandono y otros DPI/monitores siguen pendientes. Leave no prueba cancelación global. Soltar fuera después de Leave puede dejar el receptor visible hasta F9/cierre.

Originales: evidence/private/m1-native-{build,core,receiver}-03.*, native-tests/, m1-resume-*.jsonl y diarios/checks de cada run. Resumen revisado docs/verification/m1-resume.json; informe docs/reports/m1-resume.md.
Próximo experimento: F8 durante un arrastre físico desde Explorer, con liberación de tecla manteniendo el botón, y después cancelación/reentrada. Requiere gesto humano o capacidad nueva; no repetir automatización cruzada bloqueada. Consultar diario y preservar snapshot/hashes por ensayo. No repetir build/tests sin cambio ni afirmar M1 completo hasta cumplir matriz real.
CI configurada, sin ejecución remota; sin push/release ni modificación de licencia. Logs/fixtures/binarios fuera de Git.
