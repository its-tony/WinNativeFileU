# M3 — Motor inicial de conversión offline

Fecha: 2026-10-20, America/Guatemala. **M3 completado en el alcance inicial PNG/BMP descrito aquí.** Autorizado tras el cierre M2. No se ha iniciado M4.

## Summary

Motor real PNG ↔ BMP con salida nueva, inspección de datos, progreso por etapas, cancelación y errores definidos. Banco WinUI separado con imágenes generadas, previews y nombre de salida. Propietario confirmó conversión, uso del resultado y vuelta a BMP. Diario, archivos y control posterior corroboran el resultado; el radial M2 sigue siendo un receptor de fixtures sin conexión al motor.

| Tarea M3 del roadmap | Evidencia de implementación y ejecución | Límite |
|---|---|---|
| Abstracciones | IImageConverter, ImageInfo, request/result, etapas, errores y política pura; 34 tests Core | Dos formatos iniciales |
| Motor inicial | LocalImageConverter usa Windows.Graphics.Imaging; códecs ejecutados en tests y banco | Windows 11 x64 probado en este equipo |
| Detección | Firma, contenedor y decoder; BMP con extensión .data identificado correctamente en test | No detección universal por extensiones |
| Conversión | BMP24 independiente → PNG → BMP, seis píxeles/dimensiones conservados; banco real 3×2 | PNG RGB/RGBA8 sin entrelazado; BMP Windows24/32 sujeto al decoder |
| Progreso | Validating, Decoding, Encoding, Publishing, Completed, pruebas y diario real | Etapas, sin porcentaje ni benchmark |
| Errores | OutputExists real visible; corruptos, alpha, límites, rutas, colisiones y CleanupFailed probados | No auditoría exhaustiva de entradas ni filesystem adversarial |
| Tests | Locked restore/build: 92 aprobadas, 0 advertencias/errores | Cancel UI, lector y otras pantallas sin ensayo |

## Reverse Engineering Findings

No se incorporó ningún artefacto propietario ni se hizo nueva afirmación sobre Tangerine. El motor es implementación original sobre APIs públicas. Hallazgo OBSERVED de nuestro fixture: un PNG con zlib inválido y CRC reparado no falló en el decoder nativo; produjo datos transparentes y un error posterior de alpha. El test fallido queda conservado. La validación previa de zlib/filas/filtros corrige ese caso, sin convertir esta comprobación en certificación completa del estándar PNG. Fuentes primarias y decisión en [ADR0009](../decisions/0009-m3-initial-image-engine.md).

## Architecture Decisions

Core mantiene contratos/política sin I/O. Converters contiene validación local y códecs Windows; no añade biblioteca externa de imágenes. App ejecuta el trabajo fuera del hilo UI y muestra etapas, errores y previews. La inspección precede a la opción de formato compatible.

Entradas abiertas para lectura, sin compartir escritura/borrado mientras se usan. Carpeta de salida existente, temporal exclusivo propio y movimiento sin reemplazo. Colisiones iniciales, tardías y entre dos escritores no reemplazan el destino. La publicación es el punto de compromiso: cancelar antes intenta limpiar el temporal; cancelar después conserva el resultado completo. Un observador que falla no invalida la publicación. Si la limpieza falla, se informa CleanupFailed con ruta y causa.

El banco limita sus entradas iniciales a imágenes de su run, y puede reutilizar resultados. No es todavía un selector general de archivos ni el flujo drag → selección radial → conversión. WindowsIntegration no llama al motor.

## Files Changed

- Core: contratos y política ImageConversion; siete casos adicionales de política.
- Converters: LocalImageConverter y PngCrc, proyecto Windows y lockfile.
- App: ConversionWorkbenchWindow, selección de modo y referencia al motor.
- Tests: Converters.Tests con imágenes generadas y códecs reales.
- Scripts/solución: build ejecuta la tercera suite; launcher y comprobador del banco.
- Documentación: arquitectura, dependencias, decisión, tareas, bitácora y resumen de verificación.

## Tests

`pwsh -NoProfile -File scripts/build.ps1 -LockedRestore`: salida 0, **92 aprobadas: 34 Core + 33 WindowsIntegration + 25 Converters**, 0 advertencias/errores. Fuente final en `1fee7f7`; build original `m3-workbench-build-02.json` y TRX inmutables `m3-workbench-{core,integration,converters}-02.trx`. Los casos nativos OLE siguen usando callbacks directos/datos sintéticos; M3 no repitió gestos Explorer ni les atribuye conversión.

Motor: píxeles exactos y dimensiones tras BMP→PNG→BMP, Unicode, extensión engañosa, hashes de originales, destino existente/mismo origen, colisión tardía y escritores concurrentes. Cancelación en cuatro etapas, incluida Publishing con temporal presente; resultado completo conservado ante cancelación tardía y observador fallido. Rechazo de alpha para BMP, CRC/truncación/zlib, marcador APNG, límites y sintaxis de rutas. Un bloqueo real de archivo reproduce CleanupFailed y retiene deliberadamente el temporal como evidencia privada. No afirmar limpieza universal. El fixture acTL prueba rechazo del marcador, no decodificación de una animación APNG válida.

Banco real: dos Completed, PNG de 132 bytes y BMP de 78 bytes, ambos 3×2. El propietario confirmó los pasos. Antes del control automatizado solo se habían registrado las dos conversiones; un clic posterior observó y registró OutputExists. Previews visibles y estado UIA con el error real. Parser Python independiente del códec comparó las filas BGR de los BMP inicial/final: píxeles exactos. El comprobador de nombres/tamaños/hashes confirmó ambos originales intactos, dos resultados y cero temporales en ese run. Ese comprobador solo no valida píxeles, ni se midió hash del destino antes/después del clic UI; los tests del motor sí prueban preservación ante colisiones.

Cierre AltF4: Closed en diario, primera consulta aún veía host y la siguiente ya no. Snapshot `m3-workbench-final-01.jsonl`, resultado `m3-workbench-result-01.json` con referencias/hash y resumen público [m3-completion.json](../verification/m3-completion.json).

Dos clics iniciales fueron rechazados por la herramienta al detectar actividad manual. Se detuvo la estrategia tras dos rechazos; no hubo Started y no se clasificaron como fallos de conversión. Tras respuesta del propietario y nueva observación se completó el control posterior. RestoreOnly inicialmente salió 1 sin diagnóstico dentro del sandbox; con acceso Windows revisado salió 0. Se conservan ambos intentos y no se deduce fallo del compilador del primero. La suite verde no se repitió tras cambios solo documentales.

## Known Limitations

- PNG inicial: RGB/RGBA de 8 bits, sin entrelazado; sin APNG, PNG indexado/grises/16 bits. BMP Windows DIB40/108/124, 24/32 bits, sujeto a aceptación del decoder. No JPEG/WebP/AVIF/TIFF/PDF.
- Entrada ≤64 MiB, ≤16 millones de píxeles y ≤16,384 por lado. No porcentaje, throughput, memoria máxima medida ni fuzzing.
- BMP requiere opacidad total. Se solicitan orientación EXIF y sRGB al códec; resultados a 96 DPI sin copiar metadatos/perfiles. Prueba de píxeles de fixtures no demuestra fidelidad de todo perfil/orientación ni preservación documental.
- Solo rutas absolutas de unidad fija local. Rechaza sintaxis insegura y links/placeholders detectados; no es sandbox ante cambios maliciosos concurrentes, ni garantía ante cierre abrupto/pérdida de energía.
- Cancelación por etapas verificada en motor; botón Cancel y cierre durante trabajo implementados sin ensayo UI. Sin lector de pantalla, alto contraste físico ni otras pantallas. No nueva aceptación estética o de fiabilidad general.
- Radial y banco separados. Sin tray/residencia, inicio automático, empaquetado, release ni nueva auditoría de licencias/vulnerabilidades para distribución.

## Git

Rama local `codex/m3-conversion-engine`. `73e8cec` contratos, `e433109` motor/tests y `1fee7f7` banco. Cierre documental en commit separado. Licencia e historia conservadas; originales/logs/binarios privados ignorados, sin push.

## Next Milestone

M4 Background Integration requiere nueva instrucción. Antes de ofrecer conversión desde el radial deberá diseñarse y verificarse explícitamente ese enlace; M3 no lo declara implementado.
