# ADR0006 Validación de activación con botón retenido

Fecha: 2026-10-19. Alcance M1; M2 autorizado por el propietario solo después de completar M1.

El propietario confirmó repetidamente activación durante el arrastre, pero los snapshots de activación observaron el botón suelto. No se ha demostrado la causa. No se repetirá el mismo procedimiento sin un cambio que permita discriminar el resultado.

Se añade el modo optativo `-RequireHeldActivation` al launcher. Deshabilita el botón Arm target y rechaza activación por hotkey si la observación instantánea no muestra botón físico izquierdo abajo y Escape arriba. El hotkey y Armed comparten la misma muestra para evitar confundir instantes diferentes. La configuración normal conserva el prearmado. La puerta no detecta por sí sola un arrastre OLE: se exige además el gesto humano y Enter/Over/Drop real con las teclas liberadas, foco y hashes conservados.

Se registran, solo al recibir uno de los dos hotkeys permitidos, una pista de estado de botón de la cola y la edad del último mensaje. No se registran otras teclas, ni se usan hooks, polling, AttachThreadInput o inyección. GetKeyState/GetMessageTime son pistas y no prueban por sí solos la generación o demora de WM_HOTKEY. Las muestras anteriores no se reinterpretan como pruebas positivas.

Se enumera una vez la topología de pantallas al iniciar y se muestrea DPI moviendo únicamente nuestro HWND oculto, sin foco ni movimiento del cursor. Se registra también el intercambio de botones. Se extrae el cálculo de posición para comprobar escalas, área útil pequeña y coordenadas negativas/extremas. Los tests de geometría no equivalen a probar monitores físicos ausentes. La validación real se limita a configuraciones disponibles.

Fuentes primarias DOCUMENTADAS, no reproducción del fallo:

- [GetAsyncKeyState](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getasynckeystate): estado actual, posible cero por restricciones y botones físicos.
- [GetKeyState](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getkeystate): estado ligado a procesamiento de la cola.
- [GetMessageTime](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getmessagetime): tiempo del último mensaje recuperado, con wraparound.
- [EnumDisplayMonitors](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumdisplaymonitors): enumera monitores, incluidos posibles pseudo-monitores; conservar también el conteo de pantallas.

Regresiones requeridas: botón suelto, Escape y botón de prearmado no deben abrir una sesión ni permitir un drop tardío; botón retenido permite recibir fixtures. Después, build Windows y un ensayo Explorer con el modo visible. El modo no se presenta como solución de la causa original hasta obtener evidencia.
