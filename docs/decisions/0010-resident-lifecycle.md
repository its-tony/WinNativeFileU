# ADR0010 — Residencia por eventos y preferencias explícitas

Fecha: 2026-10-20. El propietario autoriza M4 al continuar tras M3. M5, push y release siguen fuera de alcance.

Se usa un HWND propio oculto de nivel superior en el hilo WinUI para Shell_NotifyIconW versión4, RegisterHotKey y TaskbarCreated. No es HWND_MESSAGE: necesita recibir el broadcast del shell. Icono original azul/rombo generado con CreateIcon, tooltip del producto y menú Open/Hide/Exit. El HICON se conserva hasta retirar el tray y se destruye al cerrar. No hooks, temporizadores periódicos, sondeo de teclado, servicio elevado ni inyección Explorer. Alternativa: añadir un paquete de tray; se evita otra dependencia para este alcance.

ResidentWindow muestra preferencias y puede ocultarse con AppWindow.Hide. Close-to-tray requiere icono disponible; si falla su creación o recuperación, la ventana permanece o vuelve visible. Exit y fin de sesión solicitan cierre real, liberan hotkey, icono, HWND, clase y ownership. TaskbarCreated vuelve a registrar el icono; no se reiniciará Explorer del propietario para ensayar esa rama. Registrar tray con éxito no prueba por sí solo clic físico del shell.

Un mutex Local derivado del root de configuración impide dos residentes para ese perfil en la sesión. Un duplicado sale, sin crear otra UI ni reenviar activación al primero. Perfiles separados pueden coexistir y producir conflicto de shortcut: ese conflicto se muestra. No se afirma instancia única entre sesiones Windows o raíces distintas.

Preferencias JSON version1: tema, cerrar a bandeja y atajo de mostrar ajustes (None, CtrlAltW, CtrlAltShiftW). El cambio registra primero el nuevo atajo; si no está disponible conserva el anterior. La persistencia escribe temporal exclusivo, flush y reemplazo del archivo propio con copia anterior única. Corrupción, versión desconocida y propiedades inesperadas se preservan; se desactiva guardar en la UI hasta reparar. No hay autosave continuo ni lectura periódica. Las copias anteriores se conservan; no hay política de poda ni sandbox contra cambios maliciosos concurrentes.

Inicio de sesión es una acción aparte, nunca inferida del JSON ni activada al cargarlo. La UI normal ofrece Enable/Disable; HKCU Run solo usa el nombre WinNativeFileU y un comando citado del ejecutable actual con argumentos residentes fijos, hasta260caracteres. Un valor observado diferente se conserva. No se promete compare-and-swap ante edición concurrente del registro. Cambiar el ejecutable puede dejar una entrada anterior que exige reparación manual; no se sobrescribe silenciosamente. Windows/políticas/Task Manager pueden deshabilitar o retrasar la ejecución. Sin garantía de arranque al logon por leer/escribir un valor.

El launcher de pruebas usa un root nuevo bajo evidence/private; allí la UI de inicio está bloqueada. Los tests de startup usan un adapter en memoria. El harness solo lee el valor propio y conserva su fingerprint privado antes/después. El sandbox denegó esa lectura en dos scripts; ambos intentos quedan registrados y se cambió al ámbito Windows revisado. No se escribió el valor real ni se hizo logoff/reboot.

M3 workbench y radial permanecen modos separados. El residente no gestiona aún trabajos de conversión ni habilita el flujo radial→motor. Las hotkeys de M4 solo recuperan ajustes, no sustituyen CtrlAltD/X del prototipo M2.

Fuentes primarias leídas, sin equiparar documentación a ejecución:

- [Shell_NotifyIconW](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyiconw) y [NOTIFYICONDATAW](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-notifyicondataw): registro/versión/callbacks/eliminación.
- [Notification area](https://learn.microsoft.com/en-us/windows/win32/shell/notification-area): ciclo del icono y mensajes del shell.
- [Windowing WinUI](https://learn.microsoft.com/en-us/windows/apps/develop/ui/windowing-overview): Window/AppWindow y visibilidad.
- [Run/RunOnce](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys): ámbito por usuario, comando/límite y condiciones de logon.
- [CreateIcon](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-createicon): creación y propiedad del HICON.
- [Shell_NotifyIconGetRect](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyicongetrect) e [identifier](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-notifyiconidentifier): consulta limitada al icono propio. Bounds no acreditan visibilidad física.

Prueba final: locked116verde (46Core45integración25conversores),0advertencias/errores. Tray/HWND/hotkey y conflictos se ejecutan realmente en tests; notificaciones allí son callbacks directos. UI real: guardado/tema/cierre a tray/atajo automatizado desde Explorer/duplicado/Exit/reinicio oculto observados. Icono genérico no encontrado por el propietario se conserva como fallo; nueva versión añade icono distinguible/GetRect. Propietario y diario confirman luego Open/Hide/Open/Exit por menú real y cinco limpiezastrue/proceso ausente. Causa anterior no determinada. TaskbarCreated real, sign-in, lector y otras pantallas no verificados. [Aceptación y límites](../reports/m4-resident.md).
