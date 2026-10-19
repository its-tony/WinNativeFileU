# ADR 0004 — Diagnóstico y reentrada de M1
Fecha: 2026-10-18 America/Guatemala. Continuación autorizada por propietario; sin M2.

El código anterior ocultaba el receptor ante cualquier DragLeave. Los diarios muestran entradas seguidas de Leave sin Drop, pero no permiten atribuir una causa específica al fallo reportado. Se cambia el comportamiento verificable: si en ese callback el botón izquierdo sigue observado abajo y Escape no lo está, se descarta la selección y se conserva el receptor esperando reentrada. Los demás casos terminan la sesión propia, sin afirmar cancelación global.

Se registran F8/F9 recibidos incluso si Arm es ignorado porque ya está abierto. Los snapshots de botón izquierdo/Escape se toman solo en eventos; no hay polling, hooks ni lectura de escritura arbitraria. La observación del botón no prueba por sí sola un arrastre OLE. El bit bajo de GetAsyncKeyState no se utiliza; el alto es una observación instantánea y puede devolver cero por restricciones de escritorio/UIPI. Escape observado es una pista, no una prueba del estado final del origen.

Fuentes primarias: [GetAsyncKeyState](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getasynckeystate), [DoDragDrop](https://learn.microsoft.com/en-us/windows/win32/api/ole2/nf-ole2-dodragdrop), [QueryContinueDrag](https://learn.microsoft.com/en-us/windows/win32/api/oleidl/nf-oleidl-idropsource-querycontinuedrag). OLE documenta que Leave puede seguir a salida de ventana o cancelación del origen.

Las pruebas nuevas usan ventanas nativas ocultas, STA y Register/Revoke reales, con productores IDataObject sintéticos y HGLOBAL CF_HDROP Unicode reales. Invocan directamente los callbacks del receptor. No muestran ventanas, registran hotkeys, inyectan entrada ni actúan sobre Explorer; no son una sustitución del gesto real. Validan decodificación, reentrada, rechazo y originales sin alteración; las pruebas de mouse/tecla y OLE desde Explorer siguen siendo una matriz distinta.

Evidencia nueva de sesión anterior: diario final ya contiene RevokeDragDrop hr0. Se conserva como snapshot privado. El propietario reanudó Computer Use; permanece el bloqueo documentado del arrastre automatizado entre ventanas, que no se reintenta.

Límites del cambio: si el botón se suelta después de abandonar el receptor, este no recibe necesariamente otro callback y puede permanecer visible hasta F9/cierre. No se añadió sondeo global para ocultarlo. Esa terminación fuera y la configuración de botones intercambiados siguen pendientes en la matriz de M1. La prueba de reentrada usa snapshots inyectados; no acredita observaciones físicas de Escape o del botón.
