# Análisis de interacción
Fuentes S1/S2; observación EXP-R0-03. No hay observación dinámica en macOS.

La web actual describe una pulsación breve durante el arrastre y persistencia de la rueda después de soltar la tecla. La ficha Apple conserva instrucciones de mantener Shift y Option+Shift. Son descripciones públicas divergentes: posible texto anterior o cambio de versión, HYPOTHESIS, sin resolver con app real. No usar el texto antiguo para afirmar que hace falta mantener la tecla.

OBSERVED solo en simulación: Enter sobre landscape.jpg abre siete opciones; flecha derecha cambia PNG por WebP; Escape cierra y devuelve foco. No se observó una conversión ni cancelación durante un arrastre real. La geometría, animaciones y latencias nativas no fueron medidas.

DOCUMENTADO: Control abre acciones que admiten carpetas y selecciones mixtas; herramientas y conversiones dependen del tipo. Persisten desconocidos el criterio exacto para lotes heterogéneos, cambio de tecla durante arrastre nativo, abandonar/reentrar, arrastre cancelado fuera del menú, resultados parciales, permisos, colisiones y mensajes de fallo.

Decisiones propias: separar apertura solicitada de recepción de archivos; mantener menú tras liberar tecla; no ejecutar en hover; confirmar solo Drop válido en acción compatible; Escape/DragLeave/invalidez descartan datos sin modificar originales. M1 debe validar cuál cancelación puede recibirse sin robar foco a Explorer.
