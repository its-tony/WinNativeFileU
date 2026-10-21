# ADR0009 — Motor inicial PNG/BMP

Fecha2026-10-20. El propietario autoriza continuar con M3 tras M2. M4/publicación fuera de alcance.

Elegimos Windows.Graphics.Imaging, ya disponible mediante el SDK Windows, para convertir imágenes PNG↔BMP con IImageConverter en Core y LocalImageConverter en Converters. Alternativas: nueva biblioteca de imágenes o WIC COM manual. La primera añade dependencias/licencias y la segunda más interop; ambas se posponen mientras el codec Windows cubra este alcance. Documentación no acredita ejecución hasta probar el motor.

Contrato: inspección por bytes y decoder, no extensión de entrada. PNG RGB/RGBA de8bits sin entrelazado, BMP Windows24/32bits; una imagen, sin APNG. Máximo64MiB de entrada,16MP/16384porlado, comprobados antes de extraer píxeles. BGRA8, orientación EXIF aplicada y normalización sRGB; nuevos resultados96DPI sin copiar metadatos/perfiles. BMP requiere opacidad completa: no descartar alpha silenciosamente. No prometer conversión lossless de metadatos o perfiles ni soporte de JPEG/WebP/AVIF/PDF.

Rutas absolutas en unidad fija local, sin UNC/dispositivos/ADS ni links/placeholders detectados. Entrada abierta read-only, compartida solo para lectura. Carpeta de salida existente; ninguna creación implícita de carpetas del usuario. Temporal GUID exclusivo en esa carpeta y publicación mediante File.Move con overwrite=false: una colisión, incluso tardía, conserva ambos originales y destino existente. Cancelación hasta el punto de publicación; después, resultado completo gana a cancelación tardía. Progreso por etapas, sin porcentajes inventados. Si falla limpieza del temporal, informar CleanupFailed y ruta; no afirmar eliminación exitosa. No se ofrece sandbox ante cambios maliciosos concurrentes del sistema de archivos ni garantía de durabilidad ante pérdida de energía.

El workbench M3 muestra entrada/formato, progreso, resultado/error y cancelación. Las pruebas usan imágenes generadas, Unicode, hashes, píxeles decodificados y colisiones/cancelación reales; nada de documentos personales. La interfaz radial M2 conserva su carácter de preview hasta una integración explícita, sin cambiar sus etiquetas en supuestas capacidades de conversión.

Fuentes primarias consultadas:

- [Imaging Windows](https://learn.microsoft.com/en-us/windows/apps/develop/media-authoring-processing/imaging): decoder/encoder, píxeles y configuración.
- [GetPixelDataAsync](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.imaging.bitmapdecoder.getpixeldataasync): formato/alpha, orientación y gestión de color.
- [BitmapEncoder](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.imaging.bitmapencoder): formatos integrados y FlushAsync.
- [PNG3](https://www.w3.org/TR/png-3/): firma, chunks/CRC y acTL para animación.
- [File.Move](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.move?view=net-10.0): no reemplazo y colisiones. No se deduce de esta API una garantía universal de atomicidad/durabilidad.

Resultado del nuevo fixture corrupto: CRC correcto no basta. Se valida también IDAT con ZLibStream, número exacto de filas y filtros0..4 antes del decoder; buffer comprimido limitado por64MiB de entrada y fila acotada. El ensayo anterior observó datos transparentes en este caso, no una excepción del codec. Se preservan build/TRX fallidos y se corrige antes de repetir. Workbench ejecuta inspección/conversión en worker; cierre durante trabajo solicita cancelación y espera limpieza.

Resultado verificado: locked build92tests verdes y banco real BMP→PNG→BMP3×2, comparación independiente de píxeles BMP, originales intactos, OutputExists y cierre normal. Cancelación por etapas probada en motor; botón Cancel/cierre ocupado sin ensayo UI. Alcance final en [informe M3](../reports/m3-completion.md).
