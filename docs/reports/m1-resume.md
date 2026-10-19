# M1 — continuación de diagnóstico y pruebas nativas
Actualizado: 2026-10-19, America/Guatemala. M2 sigue fuera de alcance.

## Summary
Se añadió un perfil seleccionable Letters (Ctrl+Alt+D/Ctrl+Alt+X) tras dos fallos de entrega física de F8. Compilación y27tests aprobados; control automatizado y dos nuevas recepciones múltiples humanas Letters corroborados. El propietario reporta activación durante drag; el snapshot de botón al activar no corrobora independientemente ese momento. La causa del fallo físico F8 continúa desconocida.
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
Comando real: pwsh -NoProfile -File scripts/build.ps1 -LockedRestore. Último resultado m1-shortcuts-build-01: salida0,0 advertencias/errores. **16/16Core +11/11integración aprobados** (10receptor nativo oculto y1rechazo de perfil inválido antes de abrir recursos). GitHub Actions no ejecutado.
Los10tests del receptor crean STA/HWND/RegisterDragDrop reales y entregan CF_HDROP Unicode en HGLOBAL real. Invocan callbacks directamente con IDataObject sintético: no son un bucle DoDragDrop ni origen Explorer. Casos: múltiples Unicode, soloMOVE, cancelación del receptor, salida/reentrada, señalEscape, formato no soportado, fuera de whitelist, exceso33, drop tardío y cancelación antes de Enter con texto actualizado. Cada uno de esos10casos compara hashes/cantidad de fixtures y exige Register/Revoke hr0. El caso adicional comprueba rechazo de perfil desconocido antes de abrir paths/recursos.
Los primeros runs se preservaron: build01 tenía4tests booleanos redundantes retirados; build02 tenía9nativos; build03 añade la regresión de textoF9. No se presenta conteo intermedio como suite final ni se repite compilación sin cambio.

## Limitations
No se repitió sky.drag cruzado: la restricción conocida sigue vigente. La herramienta tampoco ofrece tecla intercalada con mouse hold. El ensayo humano confirmó selección múltiple usando el botón antes de arrastrar; la activación por F8 durante drag sigue pendiente.
Siguen sin validación completa en Explorer: momento exacto de activación durante drag, liberación de teclas conservando drag, Escape con final del origen corroborado, rechazo mediante Drop físico, abandono y otros DPI/monitores. La reentrada física quedó corroborada en el ensayo Letters posterior. La ejecución de los primeros dos lotes cerró con RevokeDragDrop hr0, confirmado al reanudar el19 de octubre. Sin radial, motores, tray, startup, push ni release.

El19 se abrió otra sesión con el mismo build, Ready/STA/Register0 y F8/F9 disponibles. El propietario probó K1 sin Arm target y reportó **«F8 no mostró el cuadro»**; después confirmó que tampoco apareció con ratón suelto. Diario leído tras ambas respuestas: solo6eventos de arranque. Un control automatizado F8/F9 del mismo run sí produjo HotkeyReceived/Armed y CancelIdle, foco conservado y DPI144. La diferencia de entrega física/automatizada está observada; su causa no está establecida y no prueba un fallo de OLE, Fn ni del hardware. Se cerró normalmente con RevokeDragDrop0.

Dos fallos físicos cierran la estrategia F8 sin cambios. Se implementó selección entre FunctionKeys y **Letters** al lanzar, con solo el par elegido registrado. Letters usa Ctrl+Alt+D/Ctrl+Alt+X; UI/receptor/diario muestran esos nombres y Armed registra origen button/hotkey. No hooks, polling ni combinaciones arbitrarias. Perfil inválido rechazado antes de abrir recursos. El cambio sí motivó una nueva build/tests, ambos correctos según la sección anterior.

Sesión nueva Letters: registro y UI correctos, Ctrl+Alt+D automatizado desde su Explorer mostró el cuadro sin cambiar foco, Ctrl+Alt+X terminó CancelIdle. Archivos de ambos runs intactos por nombres/tamaños/SHA256. Snapshot m1-letters-control-01.jsonl conserva solo ese control automatizado.

Resultado humano posterior: **«Abrió y recibió ambos durante el arrastre»**. Nuevos eventos después del último input de herramienta corroboran activaciones Letters con origin hotkey, rechazo idle y dos recepciones reales count2/COPY1 con Unicode (14:39:22 y14:39:44 UTC-06:00); check posterior de originales pasó. Cada Armed humano observa LeftButtonDown=false y Enter posterior keys1: registrar el reporte positivo de gesto durante drag y la limitación de esa muestra instantánea; no declarar K1 completamente corroborado ni cambiar su criterio sin explicación. Se pidieron controles separados de Escape y rechazo durante tracking; pendientes. [Protocolo de aceptación física](../verification/m1-manual-protocol.md).

Cancelación: propietario informó **«Escape no terminó el arrastre»**. Nuevo diario sí corrobora reentrada real:4Leave con botón abajo/keepVisible y reentradas, seguidas de DropAccepted count2/COPY1 a14:41:53. Ctrl+Alt+X llegó después durante Tracking/botón abajo → CancelLatched → Leave con cancelled=true; no entrega posterior a ese latch, pero tampoco DropRejected físico. No confundirlo con fin seguro del origen ni con efecto de soltar dentro medido. Originales intactos. Se solicitó control Escape en Explorer solo para aislar origen/receptor; resultado pendiente.

## Evidence
Originales privados: m1-resume-old-session-final.jsonl, m1-native-build-01/02/03.json, m1-native-core-01/02/03.trx, m1-native-receiver-01/02/03.trx; diarios/fixtures únicos en native-tests/; m1-resume-launch-01/02.json y snapshots/UI nuevos. Resumen [m1-resume.json](../verification/m1-resume.json).

Recepción múltiple: m1-batch-drop-snapshot-01.jsonl (inmutable), m1-batch-operator-01.json (respuesta y método confirmado), m1-batch-result-01.json y check posterior del run. Tracker M1_BATCH VERIFICADO; M1_DESKTOP reabierto con evidencia nueva y cerrado BLOQUEADO para los criterios restantes. No se alteraron snapshots históricos ni se repitió build: no cambió el código.

Diagnóstico/perfiles: m1-k0-control-final-01.jsonl y m1-k0-control-result-01.json; m1-shortcuts-build-01.json y sus dos TRX preservados; m1-letters-control-01.jsonl y m1-letters-control-result-01.json. M1_SHORTCUTS VERIFICADO para controles automatizados; M1_DESKTOP reabierto para estrategia Letters y EN_PROGRESO esperando evidencia física. Las afirmaciones anteriores conservan el alcance histórico de sus respectivas entregas.

Resultado humano Letters en m1-letters-batch-snapshot-01.jsonl y m1-letters-batch-result-01.json, sin sobrescribir el control automatizado ni sus hashes. Cancelación completa, abandono/otros DPI y cronología de activación siguen abiertos.

El resultado posterior de reentrada/rechazo/Escape fallido está en m1-letters-cancel-snapshot-01.jsonl y m1-letters-cancel-result-01.json. Reentrada corroborada; M1_ESCAPE con un fallo y control externo pendiente. No se usaron hooks ni registro global de Escape para forzar cancelación.

## Git and Next Milestone
Corrección/reentrada en commit local bc5fd96; perfiles/launcher/regresión en39b1b16; reporte/estado aparte. Sin push. Próximo: validar control y gesto físicos Letters con evidencia nueva. **No iniciar M2 ni declarar M1 terminado.**
