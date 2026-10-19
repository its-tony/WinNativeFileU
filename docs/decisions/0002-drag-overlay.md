# ADR 0002: receptor OLE propio y validación del overlay

- Fecha: 2026-10-18 (America/Guatemala).
- Estado: aceptado como dirección de investigación y límite de arquitectura de R0/M0; experiencia durante el arrastre pendiente de validación M1.
- Alcance: no autoriza comenzar M1 ni declara implementada la integración.

## Contexto

WinNativeFileU busca recibir archivos de Explorer mediante un destino contextual mostrado por teclado durante el arrastre. Un evento de teclado no contiene la selección arrastrada. `IDropTarget::DragEnter` sí recibe el `IDataObject` cuando OLE entra en el destino registrado. Es un contrato documentado; no demuestra que mostrar un overlay a mitad del gesto produzca el flujo deseado. [DragEnter](https://learn.microsoft.com/en-us/windows/win32/api/oleidl/nf-oleidl-idroptarget-dragenter)

Los efectos de drop influyen en lo que el origen hace con sus datos. El objetivo inicial es recibir información sin modificar originales. La prioridad de WinUI 3 se conserva; no existe evidencia local que justifique sustituirlo por WPF.

## Alternativas

| Alternativa | Evaluación |
| --- | --- |
| Ventana propia que implementa `IDropTarget` | Candidato principal: proceso independiente, APIs públicas, datos entregados por el origen. Sigue abierta la combinación de hotkey, aparición, foco y hit testing. |
| Extensión/comando documentado de Shell | `IExplorerCommand` y handlers de arrastre derecho ofrecen entradas explícitas. No demuestran el gesto izquierdo + tecla; añaden registro y mantenimiento. Considerar como alternativa posterior sin inyección. |
| Armar el destino antes de arrastrar | Fallback preferente: muestra el destino antes del bucle OLE; conserva arrastrar y soltar, cambia el orden de activación. También requiere comprobación real. |
| Leer estructuras internas, inyectar en Explorer o capturar toda la escritura | Rechazado por el alcance y las restricciones del producto; no se investiga como solución. |

La comparación y las fuentes están en [windows-equivalents.md](../research/tangerine/windows-equivalents.md).

## Decisión

1. Separar el receptor OLE/Win32 y su ciclo de vida de la presentación WinUI y de la lógica de acciones. En M0 se documentan los límites; no se crea un receptor ficticio ni se muestra un estado de “archivos recibidos” sin datos reales.
2. Proponer para M1 un HWND de nuestra propiedad registrado como destino, sobre STA con bombeo de mensajes y liberación determinista. Comprobar que WinUI no lo haya registrado antes; no reemplazar silenciosamente un destino existente. [RegisterDragDrop](https://learn.microsoft.com/en-us/windows/win32/api/ole2/nf-ole2-registerdragdrop)
3. Aceptar inicialmente solo archivos físicos locales mediante `CF_HDROP`; rechazar virtuales, rutas de red y datos desconocidos. Aplicar validación de entrada y no activar hidratación de datos remotos. La compatibilidad con archivos virtuales será una decisión distinta. [Shell Clipboard Formats](https://learn.microsoft.com/en-us/windows/win32/shell/clipboard)
4. Devolver exclusivamente `COPY` si está permitido y la entrada se acepta; en los demás casos `NONE`. Nunca `MOVE` ni señales de eliminación del origen. El futuro experimento no tendrá conversores ni escritura sobre originales.
5. Evaluar presentación topmost sin tomar foco, con región receptora explícita. No equiparar alpha visual, `WS_EX_TRANSPARENT` y hit testing. Mantener un modo accesible enfocable separado del overlay del arrastre. [Extended Window Styles](https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles)
6. Evaluar primero un atajo configurable mediante `RegisterHotKey`, comprobar conflictos y no asignar definitivamente Shift/Ctrl/Alt. Un solo modificador breve necesita investigación adicional; un hook global no queda aprobado por esta decisión. [RegisterHotKey](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey)
7. Adoptar armar → arrastrar como alternativa que se probará si el gesto ideal resulta incompatible. No cambiar a esa alternativa silenciosamente ni afirmar equivalencia exacta con Tangerine.

## Justificación

El receptor propio delimita qué datos puede conocer la aplicación y evita depender del interior de Explorer. La política no destructiva reduce el riesgo para originales. La separación permite cambiar el host del overlay si aparecen fallos demostrados, sin cambiar la lógica de dominio. Estas son razones de diseño; ninguna equivale a resultados de rendimiento o estabilidad.

## Consecuencias y riesgos

El flujo completo permanece **HYPOTHESIS** hasta la prueba Windows. La documentación de OLE no garantiza que el atajo llegue durante el arrastre ni que una ventana recién mostrada reciba los callbacks necesarios. Ocultar nuestro menú tampoco cancela el arrastre del origen: puede quedar otro destino bajo el cursor. Deben registrarse y distinguirse estos finales.

STA/COM, foco, DPI mixto, archivos virtuales y accesibilidad requieren trabajo específico. Una ventana permanentemente `NOACTIVATE` no satisface por sí sola navegación accesible. La primera prueba se limitará a fixtures desechables; no se ampliará a conversión ni UI radial definitiva para ocultar limitaciones del receptor.

## Validación y criterio de revisión

No se ejecutaron pruebas para este ADR. El plan detallado y los criterios están en [feasibility.md](../research/tangerine/feasibility.md). Revisar esta decisión con trazas reales de OLE, resultados de foco/teclas y comprobación de originales después de la autorización de M1. Dos fallos equivalentes requieren detener esa estrategia y documentar otra hipótesis; una documentación favorable no elimina ese límite.
