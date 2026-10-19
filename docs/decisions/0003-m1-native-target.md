# ADR 0003 — Prototipo M1 con HWND propio
Fecha: 2026-10-18 America/Guatemala. M1 autorizado por propietario; M2 fuera de alcance.

## Viabilidad previa
DOCUMENTADO: RegisterDragDrop asocia un HWND a IDropTarget, exige OleInitialize/STA y message pump. El IDataObject llega por OLE, no por hotkey. Drop devuelve el efecto que el origen puede usar para borrar/mover datos: receptor solo COPY si permitido, NONE en cualquier rechazo.
Fuentes primarias consultadas: [RegisterDragDrop](https://learn.microsoft.com/en-us/windows/win32/api/ole2/nf-ole2-registerdragdrop), [Drop](https://learn.microsoft.com/en-us/windows/win32/api/oleidl/nf-oleidl-idroptarget-drop), [RegisterHotKey](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey), [SetWindowPos](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos), [DragQueryFileW](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-dragqueryfilew), [ReleaseStgMedium](https://learn.microsoft.com/en-us/windows/win32/api/ole2/nf-ole2-releasestgmedium).
La ruta adicional WinUI drag-drop consultada devolvió error web; no se usa como evidencia.

## Decisión
Mantener aplicación WinUI. Crear un HWND Win32 opaco independiente para aislar OLE del HWND/XAML que el framework pueda registrar. No es una migración a WPF ni un radial. Vive en STA de UI con message pump WinUI. Registrar antes de mostrar y revocar al salir.
Usar F8 provisional con MOD_NOREPEAT, sin modificadores, solo mientras corre este prototipo. F9 solicita rechazo/cierre. No capturar escritura ni instalar hooks. Si el registro del atajo falla, mostrar fallo y liberar recursos. Botón de armar permite ruta alternativa anterior al arrastre.
Mostrar sin activar, cerca del cursor y dentro del área de trabajo del monitor. Registrar foreground antes/después, DPI, callbacks y efectos en JSONL privado. Una combinación de APIs no demuestra todavía el gesto de Explorer.
Aceptar solo CF_HDROP de archivos físicos dentro de un directorio de fixtures explícito, sin rutas de red, placeholders ni reparse points. No abrir, copiar, mover, convertir ni modificar esos archivos. Política pura separada para límites/efectos.
Escape durante drag lo procesa el origen; DragLeave limpia el receptor. F9 durante tracking pone al destino en rechazo hasta Drop/Leave; no ocultarlo inmediatamente para evitar que el drop caiga en otro destino. Salir del target no cancela el arrastre del sistema. No anunciar cancelación de Explorer por cerrar nuestro menú.

## Alternativas y límites
WinUI AllowDrop: evita interop manual pero oculta parte del contrato nativo que queremos medir. HWND propio minimiza colisiones de registro y no cambia tecnología principal.
Shell: comandos/context menu no prueban tecla durante drag izquierdo. Armar antes de arrastrar es fallback explícito.
Pendiente de ejecución: registro COM real, recepción Explorer, hotkey durante drag, KeyUp, Escape, salida, originales intactos, foco y DPI. UIA/alto contraste/radial quedan fuera del prototipo; no se afirma accesibilidad final.
