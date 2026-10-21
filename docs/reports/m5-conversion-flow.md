# M5 — Flujo de conversión previo al empaquetado

Fecha: 2026-10-20, America/Guatemala. Estado: **VERIFICADO en el alcance acotado de este flujo**. Este informe cubre el enlace real elegido por el propietario antes de preparar la beta. No constituye cierre de M5 ni autorización de publicación.

## Summary

Implementado y compilado: panel residente con pickers de carpeta e imágenes, lote acotado, cola de un trabajo, progreso, resultados y cancelación; radial PNG/BMP conectado al mismo motor. Probado manualmente y corroborado: selección real→dos PNG, Explorer/radial→dos PNG y Explorer/radial→dos BMP. Seis resultados de una cuadrícula generada3×2 conservan todos los colores RGB mediante decodificación independiente. Cancel real conserva un resultado completado y termina sin temporales. Diez originales y todos los resultados anteriores intactos; cierre normal y liberación comprobados.

## Reverse Engineering Findings

No se analizaron nuevos binarios propietarios. Implementación original basada en las responsabilidades existentes y APIs públicas. La documentación Microsoft de pickers se leyó; la selección Windows y sus conversiones tienen evidencia de ejecución aparte. No se deduce compatibilidad con una app macOS ni se redistribuyen recursos suyos.

## Architecture Decisions

[ADR0011](../decisions/0011-selected-image-conversion.md): adapter OLE explícito, propiedad administrada de la selección y ejecución en worker. Los diagnósticos M1/M2 conservan su límite de fixtures; el residente permite rutas de imágenes locales elegidas por el usuario. Elegibilidad de hasta32bytes por archivo en Enter/Drop, validación completa posterior, máximo ocho archivos distintos del mismo formato. Solo COPY; aceptar el drop acredita que el trabajo se encoló, no que terminó bien.

Carpeta de salida explícita por sesión, sectores deshabilitados según destinos comunes y archivos nuevos con sufijo único. Lote secuencial; cancelación conserva resultados publicados y solicita limpiar el temporal actual. No rollback del lote ni garantía ante cambios maliciosos concurrentes. Exit espera cancelación antes de disponer recursos; esa rama ocupada permanece pendiente de ensayo.

## Files Changed

- Core/ImageBatch, Converters/ImageBatchConverter y elegibilidad de LocalImageConverter.
- WindowsIntegration/IFileDropHandler, DragOverlay e IOverlayPresentation.
- App/ImageConversionPanel, ResidentWindow y presentación radial de dos formatos.
- Tests de política, lote real y nueve límites del adapter OLE.
- Launch/check de imágenes, ADR0011, arquitectura, README, tareas y bitácora.

## Tests

`pwsh -NoProfile -File scripts/build.ps1 -LockedRestore`: salida0,0advertencias/errores, **131aprobadas:47Core54integración30conversores**. Build m5-flow-build-01.json y tres TRX exclusivos m5-flow-{core,integration,converters}-01.trx. El código no cambió tras esa ejecución; los commits organizan la misma fuente.

Nuevos casos: destinos comunes/vacíos/mixtos/inválidos/límite, lote con Unicode/hash/destinos únicos/archivo previo conservado, cancelación antes de publicar y después del primer resultado, firma desconocida/duplicados, y PNG elegible por cabecera cuyo CRC completo se rechaza sin salida. Nueve casos OLE verifican aceptación, centro, sector deshabilitado, selección cambiada, ocupado, cola rechazada, MOVE-only, demasiados archivos y cancelación. Usan HWND/registro/HGLOBAL reales y callbacks directos con adapter/presentación sintéticos; no sustituyen al gesto Explorer.

Ensayos reales: picker de carpeta, selección múltiple y primer trabajo2PNG; arrastre Explorer retenido→hotkey→radial PNG→segundo trabajo2PNG; dos PNG generados→sector BMP→trabajo2BMP. Cada uno termina con0errores. Armed observa botón retenido, foco conservado y DPI144; primer drop real keys40, sin prueba independiente de liberación intermedia de todas las teclas. El propietario confirma los resultados. Parser Python separado valida CRC/zlib/filtros de PNG y filas BMP contra seis colores conocidos, no solo dimensiones o reapertura por el mismo motor.

El rechazo solicitado en centro se registró realmente como CtrlAltX/CancelLatched/Leave sin nuevo trabajo. Otro gesto registra EscapeDowntrue/LeaveOverReceiver y el propietario confirma Escape. No atribuir DropRejected físico en centro a esos ensayos. El caso de centro sí pasa en callbacks sintéticos y tiene antecedente físico M2, de alcance histórico.

Control del picker: dos clicks mediante la ventana propietaria fallaron por bounds y propiedad del modal PickerHost; no había target modal devuelto. M5_PICKER_CONTROL queda BLOQUEADO para esa estrategia. El propietario completó selección y conversión manualmente. No son errores del conversor. Escape físico interrumpió Computer Use; se detuvo el turno y el propietario autorizó continuar. Recuperación posterior por CtrlAltW mostró el último trabajo terminado. El primer reporte humano de Cancelled no correspondía a un nuevo trabajo: los diarios previos registraban rechazo del arrastre. Se preserva esa distinción y se cambió a carga mediante picker.

Lote grande: ocho BMP4000×4000 seleccionados realmente. Convert automatizado produjo ocho PNG en7.032s; el clic Cancel llegó después y ese ensayo no demuestra cancelación. Fallo de timing conservado en m5-cancel-late-01.json. Condición nueva: selección ya cargada y dos clics humanos directos Convert→Cancel. Propietario confirma resultado; diario registra Cancelledtrue con un archivo completado y0errores, UI accesible y visible muestra Cancelled:1. La salida previa se conserva y no queda temporal. Hay15outputs en total: seis pequeños con verificación independiente de píxeles, ocho grandes completados y uno retenido tras Cancel. No se atribuye verificación independiente de píxeles a los nueve grandes.

Exit desde el botón observado termina normalmente. RevokeInputDragDrop y RevokeDragDrop devuelven0; cinco limpiezas del residente son true: hotkey, tray, HICON, HWND y clase; ResidentClosed. El primer listado aún veía la ventana durante cierre asíncrono; una consulta fresca posterior devuelve vacío. Checker Windows revisado confirma proceso ausente/Run intacto y0temporales; checker de imágenes confirma los diez inputs y hashes de todos los resultados anteriores conservados. No se ensayó cerrar durante trabajo.

Evidencia privada final: m5-flow-completion-result-01.json y snapshots inmutables m5-flow-completion-{resident,ole}-01.jsonl. Antecedentes m5-flow-picker-radial-01.json, m5-picker-control-failures-01.json, m5-cancel-late-01.json; pixel-journal-01/02.jsonl, pixels-01/02.json y checks inmutables del run. La consulta sandbox sin resultados de proceso no demuestra ausencia; el cierre usa el checker Windows revisado. No se activó startup. Revisión inicial de rutas/whitespace salida0 en m5-flow-path-review-01.json; revisión documental final registrada separadamente. [Resumen revisado](../verification/m5-conversion-flow.json).

## Limitations

Cancel del motor/lote probado automáticamente y botón Cancel corroborado en UI real. Cierre ocupado, cancelación del picker y fin de sesión sin ensayo. Cancelar el arrastre no equivale a cancelar una conversión. Pickers externos requieren interacción del propietario en este entorno. Sin benchmark de callbacks, accesibilidad con lector, alto contraste físico, otras pantallas o corpus amplio de formatos. PNG RGB/RGBA8 no interlazado/BMP24/32; BMP requiere opacidad. Máximo64MiB/16MP/16384por lado; sRGB/96DPI, sin conservar metadatos. Solo rutas locales fijas y controles detectados, sin sandbox contra carreras hostiles.

Instalador, empaquetado, auditoría de distribución/licencias, versionado de beta y preparación de release pendientes. CI configurada sin ejecución remota; prueba tray necesita shell interactivo. Sin push ni publicación.

## Git

Rama codex/m5-conversion-flow. Commits locales266cc5b (lote),f17b4bd (adapter OLE),2a0626f (panel/radial/harness), más documentación de aceptación separada; licencia existente preservada. Fuente compilada sin cambios tras build131verde.

## Next Milestone

Flujo elegido completado en el alcance descrito. Continuar instalador, documentación de usuario, CI/CD, licencias del payload, versionado y preparación de release de M5. El [inventario preliminar](../distribution-review.md) no acredita distribución final. No publicar automáticamente.
