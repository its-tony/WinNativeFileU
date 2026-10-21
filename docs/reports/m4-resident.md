# M4 — Aceptación acotada del residente

Fecha: 2026-10-20, America/Guatemala. **M4 completado en el alcance de prototipo residente descrito aquí.** Inicio de sesión real permanece sin ensayo y no se activó automáticamente. No M5, push ni release.

## Summary

Modo residente con ajustes WinUI, icono/menú de bandeja, atajo global para mostrar ajustes, persistencia local e instancia única por perfil. Cerrar ajustes puede ocultarlos; Exit termina y libera recursos. El propietario confirmó el nuevo icono azul/rombo y reapertura/salida desde su menú; diario y proceso corroboran el resultado. El arranque normal sin argumentos ahora abre ajustes residentes; los modos M1/M2/M3 permanecen separados.

| Tarea M4 | Implementado y comprobado | Pendiente/límite |
|---|---|---|
| Tray icon | Icono original azul/rombo, Shell_NotifyIcon versión4 y GetRect; propietario y diario corroboran Open/Hide/Open/Exit reales | Causa del icono genérico no encontrado sin determinar; no aprobación estética general |
| Inicio automático | Acción Enable/Disable separada, HKCU Run propio, comando citado y conflictos preservados; política/adapter de memoria probados | No se activó ni escribió Run; logon real no probado; perfil de ensayo bloquea esa UI |
| Configuración | Tema/close-to-tray/shortcut en JSON versionado; corrupción/versiones desconocidas conservadas; backup anterior exclusivo | Lectura y guardado reales Dark, recarga tras reinicio; sin migración o poda de backups |
| Hotkeys | CtrlAltW o CtrlAltShiftW o None; registro transaccional/conservación ante conflicto probados nativamente | CtrlAltW automatizado desde otra ventana restaura ajustes; teclado físico no probado |
| Optimización | Sin sondeo/timer periódico, no autosave continuo, trabajo por mensajes | Una muestra de reposo, sin benchmark ni presupuesto garantizado |
| Ciclo de vida | Ocultar/restaurar, instancia duplicada rechazada, Exit/liberación y reinicio oculto probados | TaskbarCreated real y fin de sesión no ensayados; perfiles/sesiones distintos pueden coexistir |

## Reverse Engineering Findings

Implementación original sobre APIs públicas, sin nuevos artefactos propietarios. El análisis R0 no aporta código al residente. Documentación Microsoft leída en [ADR0010](../decisions/0010-resident-lifecycle.md); ejecución real y límites se distinguen abajo.

## Architecture Decisions

Core define preferencias y política de cierre/comando de inicio. WindowsIntegration posee HWND propio, tray, menú, hotkey, mutex, almacenamiento y adapter de Run. App posee preferencias visuales y lifecycle WinUI. El HWND oculto recibe TaskbarCreated; si no recupera el icono, solicita mostrar ajustes. No se reemplaza ningún procedimiento WinUI ni se usa hook/inyección.

Preferencias por defecto: tema System, close-to-tray activo, CtrlAltW. Inicio de sesión no forma parte del JSON y nunca se activa al cargarlo. Perfil normal: LocalAppData/WinNativeFileU; launcher de ensayo: root privado aislado con inicio bloqueado. Temporal de settings exclusivo, reemplazo del archivo propio con backup único y preservación de archivos inválidos. Mutex Local por root en esta sesión; un segundo proceso sale sin reenviar activación al primero.

Registro Run solo modifica el valor WinNativeFileU tras una acción explícita en UI normal. Un valor distinto se conserva; no se ofrece compare-and-swap frente a edición concurrente. La ruta del ejecutable debe permanecer válida y Windows/políticas pueden bloquear o retrasar el inicio. La app no registra inicio automáticamente. M4 no conecta el radial al motor ni administra trabajos del workbench M3.

## Files Changed

- Core/ResidentSettings y ResidentPolicyTests: preferencias, política de cierre seguro y comando de sign-in.
- WindowsIntegration/ResidentHost, ResidentInstance, ResidentSettingsStore y StartupRegistration: servicios nativos, persistencia y adapter.
- ResidentIcon: HICON original de 32×32 generado una vez, con destrucción explícita; sin archivo propietario ni nueva dependencia gráfica.
- App/ResidentWindow y App.xaml.cs: ajustes, ocultar/mostrar/salir y selección del modo residente.
- scripts/start-resident.ps1 y check-resident.ps1: perfil nuevo/reutilizado, inicio oculto, fingerprint de Run propio y muestras del proceso.
- ResidentTests, ADR0010, estado, hipótesis, bitácora y documentación revisada. Originales privados ignorados.

## Tests

Build final: `pwsh -NoProfile -File scripts/build.ps1 -LockedRestore`, salida0, **116 aprobadas:46Core+45integración+25conversores**,0advertencias/errores. Son24casos adicionales a M3. m4-build-03.json y TRX exclusivos m4-{core,integration,converters}-03.trx. Fuente final7c7062b sin cambios tras build03; no repetir suite por documentación. Build01 no compiló por namespace de Colors y produjo warning por espera Task; ambos corregidos antes de build02 (113verdes). Evidencias originales conservadas. Sin nueva ejecución remota de CI; el test de bandeja requiere shell Windows interactivo.

Tests nuevos: política de cierre, enum/versiones/comando/límite; settings reales, backup, corrupción/propiedad desconocida/tamaño/ruta-directorio; startup con adapter de memoria; mutex entre hilos y liberación; HWND/Shell_NotifyIcon/registro real de hotkeys con conflictos, disposición y re-registro. GetRect, ciclo del HICON propio y tres casos de HRESULT fallido/bounds vacíos/no vacíos. Las notificaciones de tray/hotkey de ese test son invocaciones directas, sin equivalencia con clic de Explorer o teclado físico. El menú real se prueba aparte. No se escribe el Run real en tests.

UI real: tema Dark elegido, Save visible y registrado; archivo de settings creado. AltF4 oculta sin terminar; proceso vivo y ajustes ausentes del inventario. CtrlAltW enviado por la herramienta a una ventana Explorer produce ResidentHotkey/SettingsShown, con tema conservado. No se leyó contenido de documentos personales. Un segundo proceso mismo root sale AlreadyRunning. Exit explícito registra eliminación exitosa de hotkey, icono, HWND y clase, ResidentClosed y proceso ausente. Nuevo StartHidden carga Dark, se oculta y vuelve por CtrlAltW. Primeras listas durante cierre aún podían mostrar la ventana; consultas posteriores corroboraron ausencia.

Muestra anterior de build02 en reposo: CPU15.625ms en3019.2824ms, working set146931712bytes y1033handles; sin umbral de aceptación ni benchmark. Fingerprint del valor propio Run permaneció intacto y no quedaron temporales de settings. Dos scripts bajo sandbox no pudieron leer HKCU Run; se detuvo esa estrategia y el ámbito Windows revisado permitió comparar sin escribir. No confundir ese bloqueo del entorno con fallo de la app o con prueba de startup.

Fallo preservado: el propietario no encontró el icono genérico y el diario tenía0TrayMenu. Add/version exitosos no probaban visibilidad; causa sin determinar. Se detuvo ese ensayo y se añadió icono azul/rombo distinguible, GetRect propio y botón Check. Dos consultas S_OK/bounds no vacíos quedaron registradas; el texto visible aclara que eso no confirma visibilidad. No se usaron esas coordenadas para controlar el escritorio. El propietario después respondió «siii ya lo vi» y confirmó reapertura/salida. No atribuir causalidad exclusiva al diseño ni borrar el resultado anterior.

Ensayo final real: TrayMenu comandos **1,2,0,1,3** corresponden a Open, Hide, menú descartado, Open y Exit. El segundo Open sucede después de SettingsHidden y produce SettingsShown. Exit produce cinco resultados de limpieza true: hotkey, entrada de tray, HICON propio, HWND y clase; después ResidentClosed y proceso ausente. Check posterior confirma Run sin cambios y0temporales. No se hizo inventario UI posterior en esta fase: ausencia del proceso y resultados de destrucción son la evidencia de cierre.

Evidencia final: m4-completion-final-01.jsonl y m4-completion-result-01.json, con hashes de evidencia y conteo de TRX. Antecedentes: m4-runtime-result-01.json, m4-owner-tray-failure-01.json, m4-icon-diagnostic-01.jsonl y m4-icon-result-01.json. [Resumen público](../verification/m4-resident.json). Los resultados anteriores de Dark/hotkey/duplicado/reinicio oculto se conservan; no se repitieron todos tras el cambio de icono.

## Known Limitations

No logon, reinicio Explorer, fin de sesión, lector, alto contraste físico u otra pantalla ensayados. TaskbarCreated/fin de sesión implementados sin reproducción. No porcentaje de optimización ni garantía de consumo/rendimiento. Icono original comprobado en este ensayo, sin validación general de legibilidad/DPI. Sin migración de settings, poda de backups o sandbox ante cambios hostiles concurrentes.

Startup solo se ofrece por acción explícita; adapter real compilado pero sin escritura ni sign-in demostrados. Mover el ejecutable puede dejar entrada anterior que no se reemplaza automáticamente. Mutex por perfil/sesión, sin IPC para enfocar la instancia existente. Radial M2 y motor/workbench M3 separados; el residente no añade conversión por arrastre. Sin instalador, empaquetado, release o nueva auditoría de distribución.

## Git

Rama local codex/m4-background-integration. Commits34236b6 política,dfbd380 servicios nativos,bd0abf8 UI/harness,7c7062b icono/diagnóstico y documentación separada; sin cambios de licencia, push ni release.

## Next Milestone

M5 necesita nueva instrucción; no comienza automáticamente. La integración radial→motor sigue pendiente y debe verificarse antes de ofrecer un flujo general de conversión por arrastre.
