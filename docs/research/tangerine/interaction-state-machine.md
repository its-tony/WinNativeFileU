# Máquina de estados propuesta
HYPOTHESIS de diseño original, no implementación verificada de Tangerine. El modelo de dominio M0 solo protege el límite recepción/cancelación; el adaptador Windows se pospone a M1.

| Estado | Evento y condición | Siguiente | Efecto permitido |
|---|---|---|---|
| Idle | Tecla válida/armado explícito | AwaitingData | Solicitar mostrar HWND; no suponer archivos |
| AwaitingData | DragEnter con formato permitido | Tracking | Metadatos mínimos; no leer contenido |
| Tracking | Hover en acción compatible | Tracking | Resaltar; nunca ejecutar |
| Tracking | KeyUp | Tracking | Mantener rueda |
| Tracking | Drop, archivos y acción válidos | Received | Snapshot independiente; copiar referencias de rutas, no mover |
| AwaitingData/Tracking | Escape, invalidación o DragLeave | Idle | Ocultar y liberar referencias |
| Received | Inicio explícito de trabajo posterior | Processing (futuro) | Fuera de M0/M1 |
| Processing | Éxito/error/cancelación | Idle | Resultado explícito; preservar originales |

Un gesto de ratón sin IDataObject no es una recepción de archivos. La UI pendiente no conoce formatos antes de DragEnter. Si aparece bajo el cursor, la llegada inmediata de DragEnter y el hit testing son preguntas M1.
Procesamiento, selección de acciones, Windows key adapter y estados de progreso no están implementados en M0. El modelo mínimo no prueba OLE ni hotkeys. No se autoriza saltar a M1.

Límite crítico: Idle significa sesión propia cerrada; no prueba que Explorer haya cancelado su arrastre. Ocultar el destino puede exponer otra ventana al drop. El adaptador M1 debe distinguir y registrar esos finales; ver feasibility.md. EnterAcceptedTarget y ReceiveDrop son entradas explícitas del modelo para el futuro adaptador, sin comprobación autónoma de procedencia ni identidad de sesión.
