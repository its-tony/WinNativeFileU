# ADR0007 Presentación radial M2

Fecha: 2026-10-19. M1 cerrado en37efb9c; M2 autorizado por el propietario. M3/conversión, push y release fuera de alcance.

La primera superficie M2 es un componente WinUI enfocable con siete sectores de ejemplo, geometría y selección pura en Core. Hover selecciona; confirmación explícita actualiza solo el estado de preview. Centro/exterior/huecos no seleccionan una acción. Botones Toggle nativos exponen nombre y estado, navegación circular por flechas, Home/End, confirmación Enter/Space y Escape para limpiar. No se registran teclas globales en este modo.

La ventana de preview aplica DesktopAcrylicBackdrop si está soportado y conserva alternativa sólida para alto contraste o indisponibilidad. Temas System/Light/Dark, tamaño ajustable y layout a una columna en ventanas estrechas. Animación breve de escala respetando AnimationsEnabled y opción local Reduce motion; no cambia ajustes Windows.

Esta superficie no recibe IDataObject ni anuncia compatibilidad de formatos basada en archivos. Las etiquetas son ejemplos visuales, sin motores ni operaciones. El receptor M1 verificado se conserva por separado; conectar la presentación con OLE y verificar que no se robe foco al activar siguen siendo trabajo de integración M2. No presentar preview o tests de geometría como esa integración.

Fuentes primarias DOCUMENTADAS, no prueba local hasta compilar/ejecutar:

- [System backdrops](https://learn.microsoft.com/en-us/windows/apps/develop/ui/system-backdrops): Window.SystemBackdrop y DesktopAcrylicBackdrop; soporte/política Windows.
- [DesktopWindowXamlSource](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.hosting.desktopwindowxamlsource?view=windows-app-sdk-2.0): alternativa para alojar WinUI en HWND, todavía no incorporada al receptor.

Pruebas previstas: selección sin confirmación por hover, centro/exterior/huecos, geometría de distintos tamaños/cantidades, cancelación y navegación con wraparound. Después build Windows y revisión real de UI, temas, teclado y tamaño. No se afirma prueba de lector de pantalla, alto contraste del sistema o apariencia efectiva de Acrylic solo por compilar.

Diagnóstico real de arranque: la suscripción Windows.UI.ViewManagement.AccessibilitySettings.HighContrastChanged falló con COM0x80070490 en el constructor. Dos arranques fallidos cierran esa estrategia; el segundo preservó la excepción exacta. Se sustituye por [Microsoft.UI.System.ThemeSettings](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.system.themesettings?view=windows-app-sdk-1.7), creada para AppWindow.Id después de Loaded y desuscrita al cerrar. Backdrop se aplica tras Loaded. La [guía de contraste WinUI](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/high-contrast-themes) documenta esa API para Win32. La corrección necesita nueva build y prueba real; no se afirma que la API anterior falle universalmente.

Resultado: ThemeSettings permitió arranque. La revisión posterior encontró tema externo/contraste/estado/altura incorrectos; build04 los corrigió y nueva inspección real confirmó Dark y System/Light legibles, mensaje UIA real y estado visible.53tests aprobados. Ver ../reports/m2-visual.md para alcance y límites. Integración OLE sigue pendiente.
