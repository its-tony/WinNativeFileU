# ADR0008 — Radial sobre el receptor nativo

M2 autorizado; implementación experimental, no cierre del hito.

Se conserva DragOverlay como dueño del HWND, registro OLE, dos hotkeys, política CF_HDROP/fixtures y diario. IOverlayPresentation es opcional y se dispone antes de destruir el HWND. Sin presentación se conserva la ruta rectangular M1 y sus pruebas. App implementa RadialOverlayPresentation con DesktopWindowXamlSource; WindowsIntegration no referencia WinUI.

El island muestra el mismo control radial en un Viewbox con margen20DIP. La ventana nativa conserva WS_EX_NOACTIVATE y MA_NOACTIVATE, y se abre con SWP_NOACTIVATE. El tamaño preferido es460DIP, limitado al área útil. Las coordenadas OLE de pantalla se convierten a cliente físico, luego a DIPs y al control mediante transformaciones XAML. El hijo se considera parte del receptor para resolver Leave.

Tras validar datos locales, Enter/Over consultan el sector. Drop vuelve a consultar: centro, huecos, exterior o rechazo no permiten aceptar una selección anterior. RadialSelection registra cambios de etiqueta y DropAccepted registra selectedFormat/previewOnly. No convierte archivos ni declara capacidad real para estos formatos de ejemplo. Las rutas siguen limitadas a fixtures y COPY/NONE; no se introduce MOVE, lectura de contenidos, red ni hooks.

Esta arquitectura exige verificar el despacho real al padre cuando el puntero está sobre el hijo WinUI. No se revoca un registro interno de WinUI ni se sustituye su window procedure. Las pruebas nativas directas con presentación sintética validan la frontera, pero no resuelven esa pregunta. Si Explorer no alcanza el receptor, conservar evidencia y revisar la arquitectura antes de repetir.

Fuentes primarias DOCUMENTADAS:

- [DesktopWindowXamlSource](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.hosting.desktopwindowxamlsource?view=windows-app-sdk-2.0): alojamiento WinUI en HWND y disposición explícita.
- [Initialize](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.hosting.desktopwindowxamlsource.initialize?view=windows-app-sdk-2.0) y [SiteBridge](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.hosting.desktopwindowxamlsource.sitebridge?view=windows-app-sdk-2.0): WindowId padre y puente hijo con redimensionamiento.
- [SystemBackdrop](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.hosting.desktopwindowxamlsource.systembackdrop?view=windows-app-sdk-2.0): propiedad disponible en el island.
- [ScreenToClient](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-screentoclient) e [IsChild](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-ischild): transformación física y descendencia del HWND.
- [RegisterDragDrop](https://learn.microsoft.com/en-us/windows/win32/api/ole2/nf-ole2-registerdragdrop): registro por ventana y necesidad de bombeo de mensajes.

Evidencia local: build02 compila contra el SDK instalado y aprueba60tests,0advertencias/errores. Cuatro casos nativos nuevos usan presentación sintética; tres casos numéricos nuevos acotan ubicación radial. Runtime real observa PresentationReady/Register0/Ready, radial visible y control idle de hotkeys con foco conservado a DPI144. No acredita todavía un Drop Explorer ni una modificadora aislada. Verificación pendiente según [protocolo M2](../verification/m2-manual-protocol.md).

## Revisión tras el fallo real (EXP-M2-05)
La ruta de registro exclusivo del padre falló: propietario no pudo entregar y el diario no contiene Enter/Over/DropAccepted, incluso con activación retenida y foco Explorer conservado. No se repite sin cambios ni se presenta como integración aceptada. La causa interna de WinUI no está trazada.
Se incorpora un hijo nativo propio sobre el island, registrado mediante RegisterDragDrop y dispuesto con RevokeInputDragDrop antes de destruirlo. WS_EX_LAYERED alpha1 conserva una contribución visual de1/255; no se usa alpha0 ni WS_EX_TRANSPARENT, que dejan pasar el hit-test. El tamaño/Z-order se sincroniza después del island. InputHitTest observa si WindowFromPoint devuelve ese HWND al abrir. No se toca el registro/procedimiento del framework ni se inyecta entrada. Cualquier aceptación nueva requiere runtime y otro gesto real.
Se cambia presentación a400DIP circular, región nativa de ventana con propiedad transferida al sistema solo al aplicar con éxito, y título M2. Se añade protección de reentrada durante resize.
Fuentes primarias: [layered windows y hit testing](https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features#layered-windows), [SetLayeredWindowAttributes](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setlayeredwindowattributes), [SetWindowRgn y propiedad de región](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowrgn), [UpdateWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-updatewindow). Documentación no equivale a dispatch OLE demostrado.
