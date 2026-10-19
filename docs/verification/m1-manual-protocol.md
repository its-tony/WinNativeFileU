# M1 — protocolo de validación física

Fecha: 2026-10-19, America/Guatemala (UTC-06:00). Solo M1; no habilita M2.

Este protocolo separa el gesto humano, los callbacks del receptor y las comprobaciones de archivos. Los tests con IDataObject sintético no sustituyen estos ensayos. La automatización disponible bloquea el arrastre Explorer→otra ventana y no permite intercalar una tecla con el botón retenido; no repetir esa estrategia sin capacidad nueva.

## Preparación y evidencia

Usar `scripts/start-prototype.ps1`, conservar el `run.json` impreso y abrir **Open fixtures**. Confirmar `Ready`, STA, `RegisterDragDrop` hr0 y ambos hotkeys disponibles en el diario de ese run. Aceptar exclusivamente sus dos archivos desechables; no probar con documentos personales. El receptor registra rutas, no realiza copias ni conversiones.

Antes de cada ensayo identificar el método de activación: botón, F8 antes del arrastre o F8 durante el arrastre. Empezar con el cuadro cerrado. No pulsar **Arm target** para el caso K1. Si ocurre algo distinto al procedimiento, registrar lo que se hizo realmente.

Actualización tras K0: F8 físico tampoco apareció sin arrastre. Control automatizado F8/F9 del mismo run sí llegó al manejador; causa de diferencia de entrega sin resolver. Dos fallos físicos cierran la estrategia F8 sin cambios. Para otra vía usar `scripts/start-prototype.ps1 -ShortcutProfile Letters`: **Ctrl+Alt+D** activa, **Ctrl+Alt+X** rechaza/cierra. Aplicar K0/K1/C2 con las combinaciones mostradas, anotando perfil Letters; al soltar la combinación, liberar todas sus teclas manteniendo el ratón. Nunca presentar un ensayo Letters como éxito de F8.

Después de cada ensayo, conservar un snapshot nuevo del diario append-only y ejecutar `scripts/check-prototype-files.ps1 -RunFile <run.json>`. Exigir nombres, cantidad, tamaños y SHA256 iguales al baseline. Referenciar el check y snapshot originales mediante hash; no publicar rutas personales, handles ni capturas. Registrar resultado en tracker/experimentos/hipótesis/estado.

## Matriz de aceptación

| ID | Procedimiento físico | Evidencia necesaria y límites |
|---|---|---|
| K0 | Con ratón suelto y sin arrastre ni Arm target, pulsar F8; si aparece el cuadro, cerrarlo con F9. | Control de entrega física general, solicitado tras el fallo K1 del19. Correlacionar reporte con HotkeyReceived/Armed. No sustituirlo por la prueba automatizada del18 ni asumir que registro correcto equivale a recepción. |
| B1 | Armar con el botón antes de arrastrar ambos fixtures; soltar dentro del cuadro. | Ya verificado en dos ensayos: Enter/Over/DropAccepted count2, COPY1, Unicode y originales intactos. No satisface K1. |
| K1 | Seleccionar ambos fixtures en Explorer; iniciar arrastre; sin soltar el ratón, pulsar y soltar F8; mover ligeramente dentro del cuadro y soltar allí. | Reporte humano confirma orden y liberación de tecla manteniendo el arrastre. Diario nuevo: HotkeyReceived id8 con LeftButtonDown=true → Armed → Enter/Over → DropAccepted count2/COPY1. Foreground antes=después, target permanece después de soltar F8, originales intactos. El estado instantáneo del botón solo no demuestra el gesto; correlacionarlo con reporte y callbacks. |
| K1G | Mismo gesto usando Letters y `-RequireHeldActivation`, con botón Arm deshabilitado. | Control idle debe producir ActivationDeclined, sin Armed. Ensayo humano nuevo: HotkeyReceived/Armed comparten LeftButtonDown=true con requireHeldActivation=true; Enter/Over keys1 demuestra modificadores liberados mientras continúa el arrastre; DropAccepted count2/COPY1, foco y hashes conservados. No aceptar una muestra de botón por sí sola ni reinterpretar los snapshots antiguos. |
| C1 | Durante otro arrastre dentro del receptor, pulsar Escape antes de soltar el ratón. | Propietario confirma desaparición del indicador de arrastre y fin del origen. Diario sin DropAccepted para ese ensayo, cierre propio y hashes intactos. Leave solo no prueba Escape ni cancelación global. |
| C2 | Durante otro arrastre dentro del receptor, pulsar F9 y soltar sobre él. | Reporte de rechazo y HotkeyReceived id9/Tracking con botón abajo → CancelLatched → fin propio sin DropAccepted, originales intactos. Registrar el callback real: los dos ensayos Letters finalizaron con Leave/botón suelto/puntero sobre receptor, sin DropRejected físico. El rechazo por Drop y efecto NONE permanecen probados en callbacks sintéticos; no se presentan como medición física nueva ni cancelación del origen. Ver ajuste explícito posterior. |
| R1 | Entrar con ambos fixtures; salir del cuadro manteniendo el ratón pulsado; volver a entrar y soltar dentro. | Leave con botón abajo, receptor conservado, nueva entrada y un único DropAccepted count2/COPY1; hashes intactos. No basta la prueba sintética de reentrada. |
| O1 | Entrar y salir manteniendo el arrastre; soltar únicamente sobre el fondo del host WinNativeFileU, fuera del cuadro receptor. | Corroborar destino físico y ausencia de aceptación/operación; hashes intactos. Puede quedar el cuadro visible hasta F9/cierre, limitación conocida. No soltar en escritorio, otras carpetas ni aplicaciones. |
| D1 | Repetir K1 en cada configuración de monitor/DPI realmente disponible. | Registrar DPI, rectángulo dentro del área útil, foreground y recepción correcta por configuración. No cambiar ajustes globales ni afirmar compatibilidad con monitores ausentes. |
| L1 | Cerrar normalmente el host cuando terminen los ensayos. | RevokeDragDrop hr0 en snapshot final y ausencia de la ventana observada. Esto no es una auditoría completa de recursos ni prueba por sí solo que todos los hotkeys se liberaron. |

Los ids8/9 del diario identifican acciones de activación/rechazo; en el perfil Letters no significan teclas F8/F9. Los nuevos registros incluyen `profile`, `shortcut` y el origen `button`/`hotkey` de Armed. Examinar esos campos antes de clasificar el gesto.

Resultados Letters posteriores: dos recepciones múltiples iniciales corroboradas y reporte humano favorable de activación durante drag; los Armed observan botón suelto, por lo que K1 conserva límite de cronología. R1 corroborado por4Leave con botón abajo/keepVisible y reentradas hasta DropAccepted count2. C1 falló según propietario; control Escape en Explorer sin receptor pendiente. C2 corroboró hotkey durante Tracking/CancelLatched/Leave sin entrega posterior, pero no DropRejected físico. No presentar estos logros parciales como aceptación completa de M1.

Actualización de C1 tras aclaración: el propietario confirmó que Escape terminaba la miniatura y quedaba solo nuestro cuadro. Versión corregida según ADR0005 cierra en Leave sobre receptor o ubicación desconocida y conserva espera solo con salida observada.29tests aprobados; propietario confirmó que desaparecen ambos en2ensayos con botón abajo/Escape abajo, LeaveOverReceiver,0aceptaciones y hashes intactos. C1 verificado para ese alcance; Escape ya liberado/ubicación desconocida probados en receptor oculto. No repetir C1 satisfecho sin cambio/fallo nuevo.

R1 repetido y verificado tras hit-test: dos sesiones reales con9y1Leave conservados, todos botón abajo/puntero fuera, reentradas y recepción count2/COPY1, foco conservado/DPI144 y hashes intactos. Propietario confirma activación durante drag y recepción después de reentrar. Las5activaciones del segmento aún observan botón suelto; K1 mantiene su límite de corroboración independiente. No pedir otro K1 idéntico sin diagnóstico nuevo. C2/O1 solicitados como controles distintos; pendientes de correlacionar.

C2/O1 posteriores corroborados con alcance acotado: propietario confirma ambos. Dos rechazos durante Tracking terminan con Leave/botón suelto/puntero sobre receptor, sin aceptación por sesión. Ajuste explícito de C2: la versión anterior exigía además DropRejected físico; ese callback no apareció y se conserva como no observado. Se evalúa el objetivo funcional de rechazar, sin forzar una secuencia de instrumentación ni atribuir a OLE la causa de su ausencia. La cobertura de Drop/NONE sigue siendo sintética. No repetir el mismo gesto para obtener un callback distinto.

O1 final confirma ausencia de recepción y cierre explícito: Leave fuera conserva receptor; Ctrl+Alt+X posterior, AwaitingData/botón suelto, termina CancelIdle. Destino del soltado sobre fondo del host consta por reporte humano, no por el receptor. L1 actual: cierre normal Alt+F4, RevokeDragDrop hr0 y ventana ausente en consulta posterior. Originales intactos tras cierre. DPI observado solo144; no extrapolar a configuraciones no ensayadas.

## Interpretación y cierre

Si F8 no muestra el cuadro, cancelar el origen con Escape antes de seguir. Examinar si hubo HotkeyReceived/Armed/ArmIgnored y el reporte físico antes de cambiar código o repetir. Dos fallos equivalentes cierran la estrategia; reapertura solo con evidencia nueva.

Si hay recepción pero faltan los eventos/condiciones de K1, conservar el logro con su alcance real y dejar K1 pendiente. No convertir documentación de APIs o una compilación correcta en evidencia del gesto.

M1 permanece parcial mientras falten los criterios físicos requeridos. Mantener explícitas las configuraciones no disponibles y los defectos observados. Ningún resultado de este protocolo autoriza M2, push o release.

Actualización de alcance del propietario el19: terminar M1 y después M2. La autorización no sustituye la verificación. ADR0006 añade K1G discriminante;42tests verdes y control idle negativo ya observados, ensayo humano pendiente. Inventario real informa una pantalla DPI144: D1 queda limitado a esa configuración; tests numéricos de otras escalas no son ensayos físicos ni una afirmación de compatibilidad general.
