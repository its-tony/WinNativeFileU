# M1 — continuación de diagnóstico y pruebas nativas
Fecha: 2026-10-18, America/Guatemala. M2 sigue fuera de alcance.

## Summary
Se corrigió el cierre incondicional por DragLeave: si en ese evento se observa el botón izquierdo abajo y Escape no está abajo, se limpia la selección y se conserva el receptor para reentrada. Se añadieron registros F8/F9 recibidos, ArmIgnored y pistas instantáneas de botón/Escape. También se corrigió el texto obsoleto tras F9, observado en la ventana real.
La causa exacta del fallo original del propietario sigue sin demostrarse. **M1 continúa parcial:** estos cambios y sus tests no acreditan por sí solos el gesto completo de Explorer.

## Findings
El diario anterior terminó con RevokeDragDrop hr0; nuevo listado confirmó ventana cerrada. En la ventana nueva se observaron F8→Armed, otro F8→ArmIgnored y F9→CancelIdle. Esos eventos muestran LeftButtonDown=false, así que no se promueven a activación durante drag.
El texto de estado ahora es accesible con su contenido real. La revisión visible confirmó «Receiver closed. No file operation was performed.» después de F9. El target se ocultó y el manifiesto de fixtures siguió intacto.

Resultado humano posterior: el propietario confirmó que el cuadro recibe ambos archivos y que lo abrió con **Arm target**. El diario conserva dos secuencias Enter/Over/DropAccepted, a las 19:18:06 y 19:18:19 (UTC-06:00), ambas count2 y COPY1, con los dos fixtures incluido el nombre Unicode. Cada Armed precedió a Enter, con LeftButtonDown=false y sin nuevo HotkeyReceived. **Recepción múltiple desde Explorer con receptor prearmado verificada**; no acredita F8 durante drag. Comprobación posterior: nombres, tamaños y SHA256 intactos en ambos originales. COPY es el efecto negociado; el prototipo solo registra rutas y no realiza una copia.

## Architecture Decisions
[ADR0004](../decisions/0004-m1-diagnostics-reentry.md): snapshots en eventos, sin polling/hooks/registro de escritura; bit alto de GetAsyncKeyState con límites de permisos y observación instantánea. El estado de botón no demuestra OLE activo. Leave solo no acredita cancelación del origen.
El receptor puede seguir abierto si se suelta fuera después de Leave, hasta F9 o cierre. Ese final y los botones intercambiados siguen pendientes; no se añadió sondeo global ni se afirmó cancelación segura universal.

## Files Changed
DragOverlay/Native, PointerObservation, estado accesible WinUI, WindowsIntegration.Tests y soporte en solución/build/CI. Los tests usan ventana oculta y deshabilitan hotkeys/mostrar/mover el target; no automatizan otros procesos. Fixtures y diarios permanecen en evidence/private.

## Tests
Comando real: pwsh -NoProfile -File scripts/build.ps1 -LockedRestore. Último resultado m1-native-build-03: salida0,0 advertencias/errores. **16/16Core +10/10nativos aprobados**. GitHub Actions no ejecutado.
Los tests crean STA/HWND/RegisterDragDrop reales y entregan CF_HDROP Unicode en HGLOBAL real. Invocan callbacks directamente con IDataObject sintético: no son un bucle DoDragDrop ni origen Explorer. Casos: múltiples Unicode, soloMOVE, cancelación del receptor, salida/reentrada, señalEscape, formato no soportado, fuera de whitelist, exceso33, drop tardío y cancelación antes de Enter con texto actualizado. Cada caso compara hashes/cantidad de fixtures y exige Register/Revoke hr0.
Los primeros runs se preservaron: build01 tenía4tests booleanos redundantes retirados; build02 tenía9nativos; build03 añade la regresión de textoF9. No se presenta conteo intermedio como suite final ni se repite compilación sin cambio.

## Limitations
No se repitió sky.drag cruzado: la restricción conocida sigue vigente. La herramienta tampoco ofrece tecla intercalada con mouse hold. El ensayo humano confirmó selección múltiple usando el botón antes de arrastrar; la activación por F8 durante drag sigue pendiente.
Siguen sin validación completa en Explorer: activación durante drag, liberación de tecla conservando drag, Escape/F9 con final del origen corroborado, reentrada física, abandono y otros DPI/monitores. El cierre de la ejecución más reciente no se observó. Sin radial, motores, tray, startup, push ni release.

## Evidence
Originales privados: m1-resume-old-session-final.jsonl, m1-native-build-01/02/03.json, m1-native-core-01/02/03.trx, m1-native-receiver-01/02/03.trx; diarios/fixtures únicos en native-tests/; m1-resume-launch-01/02.json y snapshots/UI nuevos. Resumen [m1-resume.json](../verification/m1-resume.json).

Recepción múltiple: m1-batch-drop-snapshot-01.jsonl (inmutable), m1-batch-operator-01.json (respuesta y método confirmado), m1-batch-result-01.json y check posterior del run. Tracker M1_BATCH VERIFICADO; M1_DESKTOP reabierto con evidencia nueva y cerrado BLOQUEADO para los criterios restantes. No se alteraron snapshots históricos ni se repitió build: no cambió el código.

## Git and Next Milestone
Corrección/pruebas en commit local bc5fd96; reporte/estado aparte. Sin push. Próximo: completar diagnóstico del gesto real con evidencia nueva. **No iniciar M2 ni declarar M1 terminado.**
