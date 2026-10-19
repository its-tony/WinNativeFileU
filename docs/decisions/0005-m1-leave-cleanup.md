# ADR 0005 — cierre del receptor tras Leave sin salida del puntero

Fecha: 2026-10-19, America/Guatemala. Solo M1.

El propietario confirmó que Escape cancela en Explorer sin receptor y aclaró el síntoma anterior: desaparecía la miniatura arrastrada, pero quedaba el cuadro blanco. Se conserva el reporte inicial y su aclaración; no se atribuye ahora un fallo demostrado a la cancelación de Explorer.

La decisión anterior conservaba el receptor cuando el botón se observaba abajo y Escape arriba en DragLeave. Análisis estático: si el origen cancela y la lectura de Escape ya es cero, esa condición permite un cuadro residual. Es una reproducción de la decisión propia, no un trazado histórico del mensaje Escape; los diarios anteriores no registran posición del puntero en Leave.

DOCUMENTADO: [DragLeave](https://learn.microsoft.com/en-us/windows/win32/api/oleidl/nf-oleidl-idroptarget-dragleave) se llama tanto por salida del destino como por cancelación. El origen decide el fin del gesto mediante [QueryContinueDrag](https://learn.microsoft.com/en-us/windows/win32/api/oleidl/nf-oleidl-idropsource-querycontinuedrag). La lectura alta de [GetAsyncKeyState](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getasynckeystate) describe el instante de la llamada, no una pulsación pasada de Escape.

Decisión: conservar espera de reentrada solo si, además de las condiciones anteriores, se observa el puntero sobre otra ventana. Si Leave ocurre aún sobre el HWND receptor, cerrar su sesión y ocultarlo; también cerrar si la ubicación es desconocida. No anunciar cancelación global por esa decisión. El caso de salida real con botón abajo conserva reentrada.

La comprobación usa GetCursorPos y [WindowFromPoint](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-windowfrompoint), una vez por Leave. El receptor actual no tiene HWND hijos. El resultado es true/false/null y queda en diario; no hay polling, captura global de Escape, tecla sintética, inspección de selección ni modificación del origen. Los tests ocultos inyectan esa observación sin mostrar ventanas ni mover el puntero real.

Regresiones: Leave con botón abajo, Escape ya arriba y puntero aún sobre el receptor debe terminar Idle y rechazar Drop tardío; ubicación desconocida debe cerrar; salida real conserva reentrada y recepción. Compilación y resultados se registran aparte, sin inferir éxito físico por los tests.

Límites: el hit-test es una muestra en el momento del callback, puede verse afectado por movimiento/oclusiones y no identifica la causa global del Leave. Si el origen termina después de una salida real sin callback nuevo, el receptor todavía puede permanecer visible hasta el atajo de rechazo o cierre. No se declara resuelto ese caso ni el gesto de activación completo.
