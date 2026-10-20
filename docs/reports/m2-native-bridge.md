# M2 — Puente radial nativo en validación

Implementado en9147737. Estado: parcial, M2 no completo. El preview visual anterior está verificado; ahora existe una presentación WinUI dentro del HWND/OLE nativo. Las etiquetas siguen siendo ejemplos y no se realiza conversión.

Build Windows m2-ole-build-02.json: salida0,0advertencias/errores,60tests aprobados (27Core+33integración). Cuatro pruebas nuevas verifican con presentación sintética sector aceptado, centro inicial seguido de sector, sector seguido de Drop en centro y rechazo explícito; tres casos numéricos cubren ubicación radial. El conjunto anterior de fixtures/efectos/cancelación sigue aprobado. No equivale a despacho Explorer ni a pruebas físicas de escalas.

Runtime observado: PresentationReady, RegisterDragDrop0, Ready STA y ambos hotkeys Letters registrados. Captura real muestra radial460DIP; apertura Ctrl+Alt+D en reposo mantiene foreground antes=después y Ctrl+Alt+X termina CancelIdle. DPI144, área útil y dimensiones690px registrados. Gate apagado para este control. La ventana principal conserva por ahora etiquetas históricas M1, aunque las instrucciones identifican el experimento M2.

Evidencia privada: m2-ole-build-01.json conserva primera ejecución sandbox con salida1 vacía/sin artefactos nuevos, sin causa atribuida; m2-ole-build-02.json conserva la ejecución Windows revisada exitosa. TRX inmutables m2-ole-core-02.trx/m2-ole-integration-02.trx. Snapshot m2-ole-ready-01.jsonl y resultado m2-ole-ready-result-01.json conservan el control idle. No se publica ningún identificador de ventanas, ruta personal o hash de fixture aquí.

Ensayo preparado: carpeta nueva con dos fixtures, receptor cerrado y solicitud al propietario para activar durante arrastre y soltar sobre PNG. Sigue pendiente correlacionar su respuesta con Enter/Over, RadialSelection, DropAccepted selectedFormat PNG/count2/COPY1 y hashes. La automatización disponible no permite arrastre entre ventanas; estrategia bloqueada de M1 no se repite. No afirmar que OLE sobre el hijo WinUI funciona antes del resultado real.

Después: centro/huecos, rechazo, reentrada/Escape, gate retenido, cierre normal/disposición del island y revisión de límites según [protocolo](../verification/m2-manual-protocol.md). Alto contraste y otras configuraciones físicas quedan pendientes donde no haya entorno disponible. M3 permanece fuera de alcance.
