# Viabilidad del gesto en Windows 11

Fecha: 2026-10-18, America/Guatemala. Estado: investigación R0; **no existe aquí una validación `VERIFIED IN WINDOWS PROTOTYPE`**. M0 prepara la base; M1 necesita autorización posterior. No se ha ejecutado Tangerine en macOS ni se deduce su implementación a partir de APIs de Windows.

## Conclusión de R0

**DOCUMENTADO:** Windows dispone de recepción OLE mediante HWND/`IDropTarget`, activación por hotkey, control de orden Z y ventanas sin activación. Los contratos se detallan con fuentes primarias en [windows-equivalents.md](windows-equivalents.md).

**HYPOTHESIS:** combinar estos mecanismos puede aproximar “arrastrar desde Explorer → tecla → overlay → soltar”. La documentación de cada pieza no demuestra la combinación ni que una pulsación sola de Shift/Alt/Ctrl sea segura. El mayor riesgo es recibir un arrastre ya iniciado sin cambiar foco, efectos nativos o destino accidentalmente. Viabilidad conceptual parcial; viabilidad de la experiencia completa pendiente.

## Riesgos y mitigación propuesta

| Riesgo | Evidencia y nivel | Consecuencia | Mitigación propuesta / criterio posterior |
| --- | --- | --- | --- |
| Mostrar el destino bajo el cursor durante el drag no genera inmediatamente `DragEnter`. | **HYPOTHESIS**; OLE selecciona el destino bajo el cursor según [DoDragDrop](https://learn.microsoft.com/en-us/windows/win32/api/ole2/nf-ole2-dodragdrop). | Overlay visible sin archivos ni acción válida. | Medir aparición y transición al mover el cursor; no mostrar acciones compatibles antes de recibir datos. Si falla la ruta, evaluar armar antes de arrastrar. |
| El atajo no llega o interfiere con copiar/mover/enlazar. | **DOCUMENTADO**: [RegisterHotKey](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey) puede fallar; [DragEnter](https://learn.microsoft.com/en-us/windows/win32/api/oleidl/nf-oleidl-idroptarget-dragenter) incluye modificadores. | El gesto ideal puede requerir otra tecla/combinación. | Atajo configurable, error visible y registro sin datos de escritura. Probar un candidato no conflictivo; no instalar hooks por anticipado. |
| La ventana toma foco o no recibe OLE. | **DOCUMENTADO**: [SetWindowPos](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos) y [estilos](https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles); combinación **HYPOTHESIS**. | Interrupción del arrastre o interacción inconsistente. | HWND propio, presentación sin activación; medir foreground antes/durante/después y secuencia de callbacks. Conservar WinUI como primera opción; no cambiar a WPF sin evidencia. |
| Confundir transparencia visual y paso de entrada. | **DOCUMENTADO**: [WM_NCHITTEST](https://learn.microsoft.com/en-us/windows/win32/inputdev/wm-nchittest) limita `HTTRANSPARENT` al mismo hilo; `WS_EX_TRANSPARENT` describe orden de pintura. | Zonas que no aceptan el drop o lo entregan al destino equivocado. | Superficie receptora explícita y zona neutral con `NONE`. Verificar centro, bordes, huecos y salida. |
| Bloquear el STA de OLE o registrar un HWND ya registrado por el framework. | **DOCUMENTADO**: [RegisterDragDrop](https://learn.microsoft.com/en-us/windows/win32/api/ole2/nf-ole2-registerdragdrop), [OleInitialize](https://learn.microsoft.com/en-us/windows/win32/api/ole2/nf-ole2-oleinitialize). | Congelar la interacción del origen o fallar el registro. | Callbacks breves, message pump operativo, comprobar HRESULT y titularidad del HWND. Revocar registro y liberar recursos al cerrar. |
| Datos virtuales, rutas no accesibles, red o archivos que cambian. | **DOCUMENTADO**: [Shell Clipboard Formats](https://learn.microsoft.com/en-us/windows/win32/shell/clipboard); validación concreta **pendiente**. | Prometer compatibilidad que no existe, bloquear lectura o provocar acceso de red. | Primer receptor limitado a archivos físicos locales de prueba; rechazar virtuales/red y no hidratar placeholders. Límites de número/rutas; nunca ejecutar el contenido. |
| Anunciar `MOVE` y causar eliminación o movimiento del origen. | **DOCUMENTADO**: [DoDragDrop](https://learn.microsoft.com/en-us/windows/win32/api/ole2/nf-ole2-dodragdrop), [IDropTarget::Drop](https://learn.microsoft.com/en-us/windows/win32/api/oleidl/nf-oleidl-idroptarget-drop). | Pérdida o alteración de originales. | Política `COPY`/`NONE`, respetando la máscara permitida. Ninguna escritura/conversión en M1. Comparar hashes y listado antes/después. |
| Confundir cierre del menú con cancelación del origen. | **DOCUMENTADO**: el origen decide mediante [QueryContinueDrag](https://learn.microsoft.com/en-us/windows/win32/api/oleidl/nf-oleidl-idropsource-querycontinuedrag). | Soltar tras ocultar el overlay puede ejecutar una operación nativa sobre otra ventana. | Escape y `DragLeave` limpian estado propio; no simular cancelación ni botón soltado. Verificar el final real del drag y comprobar originales. |
| DPI distinto, posiciones negativas o borde del área de trabajo. | **DOCUMENTADO**: [guía DPI](https://learn.microsoft.com/en-us/windows/win32/hidpi/high-dpi-desktop-application-development-on-windows). | Sectores desplazados o fuera de pantalla. | PMv2, transformaciones DIP/píxel explícitas, límites del monitor del cursor y geometría con signo; probar 100/150/200 % y monitor secundario. |
| Un overlay sin foco no permite una experiencia accesible completa. | **DOCUMENTADO**: [estilos](https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles), [accesibilidad](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/accessibility-overview). | Imposibilidad de operar con teclado/Narrator. | Diseñar alternativa enfocables con controles estándar; validar nombres, foco, contraste y cancelación. No afirmar accesibilidad por usar XAML. |
| Residencia y respuesta visual consumen recursos. | **HYPOTHESIS**, sin mediciones. | CPU/memoria/latencia mayores que los objetivos. | Medir más adelante reposo, tiempo de aparición y callbacks; no sondear permanentemente todo el escritorio ni hacer conversiones en hover. |

## Puerta de entrada al futuro M1

Plan de validación, **no pruebas ejecutadas**:

1. Registrar la versión/build real de Windows, .NET/Windows App SDK, arquitectura, monitores, escalas y nivel de integridad. Preparar únicamente archivos desechables locales y conservar hashes/listado iniciales en evidencia privada.
2. Confirmar registro de un destino OLE básico y recepción `CF_HDROP` desde Explorer, con un archivo y selección múltiple Unicode. Registrar HRESULT, formatos ofrecidos/aceptados, callback y máscara de efectos. No registrar contenido sensible.
3. Durante un arrastre real, pulsar el candidato de atajo, mostrar la ventana sin activación y entrar en su superficie. Confirmar `DragEnter` → `DragOver` → `Drop` o `DragLeave`, además del foco y efecto devuelto. No sustituir recepción por lectura de la selección de Explorer.
4. Probar Escape, salida y reentrada, soltar sin acción, formato rechazado, fuente que no permita `COPY`, pérdida de foco y cierre de la aplicación. Comprobar que el cierre propio no se presenta como cancelación del origen.
5. Comparar hashes y presencia de originales después de cada caso. Cualquier cambio inesperado impide aceptar la estrategia hasta explicar su causa. No ejecutar conversiones para aparentar que la integración funciona.
6. Probar posiciones de bordes y DPI mixto, incluyendo coordenadas negativas. Registrar las configuraciones que realmente se hayan probado; si no hay segundo monitor, declarar ese límite.
7. Si el flujo ideal falla, registrar causa y evidencia y ensayar el fallback armar → arrastrar como experimento diferente. Aplicar el límite de dos fallos equivalentes y el tracker heredado; no repetir sin nueva evidencia.

La aceptación de M1 exigirá archivos válidos recibidos, cancelación segura y originales sin alteración, con registro reproducible y una demostración real en Windows. La disponibilidad de APIs, una compilación o pruebas unitarias de estados no sustituyen esa demostración. El radial final, Acrylic, conversión, tray y startup pertenecen a etapas posteriores.

## Límites abiertos

Actualización de seguimiento: el prototipo M1 y sus resultados parciales están en [reporte M1](../../reports/m1.md). Este documento conserva el análisis previo de R0; no convierte sus hipótesis en validaciones del gesto completo.

- Ninguna medida de CPU, memoria, latencia, estabilidad o accesibilidad está disponible en R0.
- La ruta Shell es una alternativa documentada; no se ha registrado ninguna extensión ni modificado Explorer. Su despliegue y compatibilidad específicos con Windows 11 necesitan investigación posterior.
- No se ha decidido un atajo definitivo ni se ha justificado un hook global. Una pulsación breve de modificador queda pendiente de diseño y evidencia.
- La accesibilidad durante el arrastre y el modo sin arrastre necesitan validación separada.
- Se intentaron consultar dos rutas de Microsoft (`/windows/win32/shell/drag-and-drop` y `/windows/win32/winauto/uiauto-implementingproviders`) que no resultaron accesibles mediante la herramienta web; no se usaron como evidencia. Las páginas enlazadas en los hallazgos sí devolvieron contenido técnico consultable.
