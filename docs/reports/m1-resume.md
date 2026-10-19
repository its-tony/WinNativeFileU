# M1 — continuación de diagnóstico y pruebas nativas
Actualizado: 2026-10-19, America/Guatemala. M2 sigue fuera de alcance.

## Summary
Se corrigió el cuadro residual tras terminar el arrastre: Leave sobre el receptor o con ubicación desconocida cierra su sesión; solo una salida observada permite esperar reentrada.29tests pasan y dos ensayos humanos de Escape cerraron miniatura y cuadro sin entregar archivos ni alterar originales. La carrera de Escape ya liberado se cubre con regresión oculta; los dos ensayos físicos observaron Escape abajo.
Se añadió un perfil seleccionable Letters (Ctrl+Alt+D/Ctrl+Alt+X) tras dos fallos de entrega física de F8. Esa entrega pasó27tests antes de ampliar la suite de cierre a29; control automatizado y recepciones múltiples humanas Letters corroborados. El propietario reporta activación durante drag; el snapshot de botón al activar no corrobora independientemente ese momento. La causa del fallo físico F8 continúa desconocida.
Se había corregido el cierre incondicional por DragLeave para permitir reentrada con botón abajo/sin Escape observado; el cambio posterior añade la condición de salida observada y cierra los demás casos. Se añadieron registros de atajos, ArmIgnored y pistas instantáneas; el texto obsoleto tras rechazo ya está corregido.
La causa exacta del fallo original del propietario sigue sin demostrarse. **M1 continúa parcial:** estos cambios y sus tests no acreditan por sí solos el gesto completo de Explorer.

## Findings
El diario anterior terminó con RevokeDragDrop hr0; nuevo listado confirmó ventana cerrada. En la ventana nueva se observaron F8→Armed, otro F8→ArmIgnored y F9→CancelIdle. Esos eventos muestran LeftButtonDown=false, así que no se promueven a activación durante drag.
El texto de estado ahora es accesible con su contenido real. La revisión visible confirmó «Receiver closed. No file operation was performed.» después de F9. El target se ocultó y el manifiesto de fixtures siguió intacto.

Resultado humano posterior: el propietario confirmó que el cuadro recibe ambos archivos y que lo abrió con **Arm target**. El diario conserva dos secuencias Enter/Over/DropAccepted, a las 19:18:06 y 19:18:19 (UTC-06:00), ambas count2 y COPY1, con los dos fixtures incluido el nombre Unicode. Cada Armed precedió a Enter, con LeftButtonDown=false y sin nuevo HotkeyReceived. **Recepción múltiple desde Explorer con receptor prearmado verificada**; no acredita F8 durante drag. Comprobación posterior: nombres, tamaños y SHA256 intactos en ambos originales. COPY es el efecto negociado; el prototipo solo registra rutas y no realiza una copia.

## Architecture Decisions
[ADR0004](../decisions/0004-m1-diagnostics-reentry.md): snapshots en eventos, sin polling/hooks/registro de escritura; bit alto de GetAsyncKeyState con límites de permisos y observación instantánea. El estado de botón no demuestra OLE activo. Leave solo no acredita cancelación del origen.
El receptor puede seguir abierto si se suelta fuera después de Leave, hasta F9 o cierre. Ese final y los botones intercambiados siguen pendientes; no se añadió sondeo global ni se afirmó cancelación segura universal.

[ADR0005](../decisions/0005-m1-leave-cleanup.md): aclaración humana confirmó que Escape acababa con la miniatura, pero quedaba el receptor. GetAsyncKeyState no recuerda la pulsación pasada. Se observa WindowFromPoint/GetCursorPos una vez por Leave; solo puntero fuera permite conservar espera. Encima o ubicación desconocida cierra sin interceptar Escape ni modificar el origen. Las muestras de ubicación y tecla son pistas, no una API de estado global del arrastre.

## Files Changed
DragOverlay/Native, PointerObservation, estado accesible WinUI, WindowsIntegration.Tests y soporte en solución/build/CI. Los tests usan ventana oculta y deshabilitan hotkeys/mostrar/mover el target; no automatizan otros procesos. Fixtures y diarios permanecen en evidence/private.

## Tests
Comando real: pwsh -NoProfile -File scripts/build.ps1 -LockedRestore. Último resultado m1-cleanup-green-01: salida0,0 advertencias/errores. **16/16Core +13/13integración aprobados** (12receptor nativo oculto y1rechazo de perfil inválido antes de abrir recursos). GitHub Actions no ejecutado.
Los12tests del receptor crean STA/HWND/RegisterDragDrop reales y entregan CF_HDROP Unicode en HGLOBAL real. Invocan callbacks directamente con IDataObject sintético: no son un bucle DoDragDrop ni origen Explorer. Casos anteriores: múltiples Unicode, soloMOVE, cancelación del receptor, salida/reentrada, señalEscape, formato no soportado, fuera de whitelist, exceso33, drop tardío y cancelación antes de Enter con texto actualizado. Casos nuevos: Leave con botón abajo/Escape arriba sobre receptor, y ubicación desconocida; cierran y rechazan Drop tardío. Cada caso comprueba hashes/cantidad de fixtures y Register/Revoke hr0. El caso adicional rechaza perfil desconocido antes de abrir recursos.

Reproducción negativa: decisión anterior compiló y falló exactamente los2casos nuevos, Idle esperado/AwaitingData real (m1-cleanup-red-02). Tras corregirla pasan29. La invocación enfocada previa quedó sin informe/TRX y se interrumpió; no se atribuye a ella reproducción ni resultado de tests. Se conservan evidencia de interrupción y los TRX rojo/verde.
Los primeros runs se preservaron: build01 tenía4tests booleanos redundantes retirados; build02 tenía9nativos; build03 añade la regresión de textoF9. No se presenta conteo intermedio como suite final ni se repite compilación sin cambio.

## Limitations
No se repitió sky.drag cruzado: la restricción conocida sigue vigente. La herramienta tampoco ofrece tecla intercalada con mouse hold. El ensayo humano confirmó selección múltiple usando el botón antes de arrastrar; la activación por F8 durante drag sigue pendiente.
Siguen sin validación completa en Explorer: corroboración independiente del momento exacto de activación durante drag y otros DPI/monitores. El rechazo funcional y soltado exterior ya se correlacionaron según el detalle posterior, con límites explícitos: no hubo DropRejected físico y el destino externo consta por reporte humano. La reentrada física se repitió tras el último hit-test; Escape tuvo dos ensayos correctos. El host actual cerró normalmente con RevokeDragDrop hr0 y ventana ausente. Sin radial, motores, tray, startup, push ni release.

El19 se abrió otra sesión con el mismo build, Ready/STA/Register0 y F8/F9 disponibles. El propietario probó K1 sin Arm target y reportó **«F8 no mostró el cuadro»**; después confirmó que tampoco apareció con ratón suelto. Diario leído tras ambas respuestas: solo6eventos de arranque. Un control automatizado F8/F9 del mismo run sí produjo HotkeyReceived/Armed y CancelIdle, foco conservado y DPI144. La diferencia de entrega física/automatizada está observada; su causa no está establecida y no prueba un fallo de OLE, Fn ni del hardware. Se cerró normalmente con RevokeDragDrop0.

Dos fallos físicos cierran la estrategia F8 sin cambios. Se implementó selección entre FunctionKeys y **Letters** al lanzar, con solo el par elegido registrado. Letters usa Ctrl+Alt+D/Ctrl+Alt+X; UI/receptor/diario muestran esos nombres y Armed registra origen button/hotkey. No hooks, polling ni combinaciones arbitrarias. Perfil inválido rechazado antes de abrir recursos. El cambio sí motivó una nueva build/tests, ambos correctos según la sección anterior.

Sesión nueva Letters: registro y UI correctos, Ctrl+Alt+D automatizado desde su Explorer mostró el cuadro sin cambiar foco, Ctrl+Alt+X terminó CancelIdle. Archivos de ambos runs intactos por nombres/tamaños/SHA256. Snapshot m1-letters-control-01.jsonl conserva solo ese control automatizado.

Resultado humano posterior: **«Abrió y recibió ambos durante el arrastre»**. Nuevos eventos después del último input de herramienta corroboran activaciones Letters con origin hotkey, rechazo idle y dos recepciones reales count2/COPY1 con Unicode (14:39:22 y14:39:44 UTC-06:00); check posterior de originales pasó. Cada Armed humano observa LeftButtonDown=false y Enter posterior keys1: registrar el reporte positivo de gesto durante drag y la limitación de esa muestra instantánea; no declarar K1 completamente corroborado ni cambiar su criterio sin explicación. Se pidieron controles separados de Escape y rechazo durante tracking; pendientes. [Protocolo de aceptación física](../verification/m1-manual-protocol.md).

Cancelación, reporte inicial: propietario informó **«Escape no terminó el arrastre»**. Diario corrobora reentrada real:4Leave con botón abajo/keepVisible y reentradas, seguidas de DropAccepted count2/COPY1 a14:41:53. Ctrl+Alt+X llegó después durante Tracking/botón abajo → CancelLatched → Leave con cancelled=true; no entrega posterior a ese latch, pero tampoco DropRejected físico. No confundirlo con fin seguro del origen ni con efecto de soltar dentro medido. Originales intactos. El control posterior permitió aclarar ese reporte.

Aclaración posterior: **Escape cancela en Explorer solo; dentro desaparecía la miniatura y quedaba el cuadro blanco**. El defecto reportado se acota a limpieza del receptor, sin borrar el reporte inicial. Versión corregida: propietario confirma **«Desaparecen ambos»**; dos callbacks14:58:13 y14:58:19 observan botón abajo, Escape abajo y puntero sobre receptor → keepVisible=false/LeaveOverReceiver.0DropAccepted, hashes intactos. M1_ESCAPE VERIFICADO para esos gestos; no se presenta el caso exacto Escape ya liberado como reproducido físicamente.

## Evidence
Originales privados: m1-resume-old-session-final.jsonl, m1-native-build-01/02/03.json, m1-native-core-01/02/03.trx, m1-native-receiver-01/02/03.trx; diarios/fixtures únicos en native-tests/; m1-resume-launch-01/02.json y snapshots/UI nuevos. Resumen [m1-resume.json](../verification/m1-resume.json).

Recepción múltiple: m1-batch-drop-snapshot-01.jsonl (inmutable), m1-batch-operator-01.json (respuesta y método confirmado), m1-batch-result-01.json y check posterior del run. Tracker M1_BATCH VERIFICADO; M1_DESKTOP reabierto con evidencia nueva y cerrado BLOQUEADO para los criterios restantes. No se alteraron snapshots históricos ni se repitió build: no cambió el código.

Diagnóstico/perfiles: m1-k0-control-final-01.jsonl y m1-k0-control-result-01.json; m1-shortcuts-build-01.json y sus dos TRX preservados; m1-letters-control-01.jsonl y m1-letters-control-result-01.json. M1_SHORTCUTS VERIFICADO para controles automatizados; M1_DESKTOP EN_PROGRESO para la matriz física restante. Las afirmaciones históricas conservan el alcance de sus respectivas entregas.

Resultado humano Letters en m1-letters-batch-snapshot-01.jsonl y m1-letters-batch-result-01.json, sin sobrescribir el control automatizado ni sus hashes. Cancelación completa, abandono/otros DPI y cronología de activación siguen abiertos.

El reporte inicial de reentrada/rechazo/Escape está en m1-letters-cancel-snapshot-01.jsonl y m1-letters-cancel-result-01.json. Se conserva ese contexto previo a la aclaración; no se usaron hooks ni registro global de Escape para forzar cancelación.

El historial anterior queda acotado por m1-escape-clarification-01.json; la corrección/red/green están en m1-cleanup-*.json/TRX. Nueva verificación física en m1-cleanup-escape-success-01.jsonl y m1-cleanup-escape-result-01.json. La ausencia de Drop se combina con reporte humano y hashes; no se infiere fin global por Leave solo.

Reentrada física del build corregido: propietario confirmó «Sí, activó durante el arrastre y recibió ambos tras reentrar». Dos sesiones a15:06:03 y15:06:24 terminan en DropAccepted count2/COPY1 tras9y1Leave conservados con botón abajo/puntero fuera, nuevas entradas keys1 y drop keys0. Foco conservado, DPI144, originales intactos. R1 verificado con el hit-test actual. Snapshot m1-cleanup-reentry-01.jsonl y resultado m1-cleanup-reentry-result-02.json (corrige transcripción de acentos y nota inicial de conteo; originales anteriores conservados).

El reporte humano confirma el orden de activación durante drag; los5Armed nuevos observan botón suelto, de modo que la corroboración independiente de ese instante sigue parcial. No se descarta el reporte ni se repetirá K1 sin diagnóstico nuevo. C2/O1 solicitados por separado; no cambió código ni se repitió la suite29verde.

Resultado posterior C2/O1: propietario respondió «el 1 si, ahora el  2 pues tambien». Dos Ctrl+Alt+X durante Tracking/botón abajo terminaron en Leave con cancelled=true/botón suelto/puntero sobre receptor, sin aceptación en esas sesiones. No se observó DropRejected físico. El protocolo explicita ahora la diferencia entre rechazo funcional corroborado y aquella expectativa de callback sin observar; NONE/Drop siguen cubiertos por tests sintéticos, sin atribuir una regla de despacho a OLE ni repetir el gesto para forzar el evento.

En O1 final, Leave fuera con botón abajo conservó el cuadro; Ctrl+Alt+X posterior en AwaitingData/botón suelto cerró CancelIdle, sin recepción en esa sesión. El destino sobre fondo del host y soltado exterior constan por confirmación humana, no por telemetría del origen. Un DropAccepted adicional a15:10:26 pertenece a otro ensayo; se conserva fuera de esos resultados negativos. Nombres/tamaños/SHA256 intactos. Cierre normal Alt+F4, RevokeDragDrop hr0 a15:12:46 y ventana ausente en consulta posterior. Snapshot final m1-reject-outside-final-01.jsonl y resultado m1-reject-outside-result-01.json; no se reescribieron snapshots anteriores.

## Git and Next Milestone
Corrección/reentrada en commit local bc5fd96; perfiles/launcher/regresión en39b1b16; cierre del receptor residual y2regresiones en bbbd8d0; reentrada actual documentada en db5e9a2. Sin push. Próximo: analizar la discrepancia de activación antes de nuevos ensayos y documentar el alcance de DPI144. C2/O1/Escape/reentrada ya correlacionados; host cerrado. **No iniciar M2 ni declarar M1 terminado.**
