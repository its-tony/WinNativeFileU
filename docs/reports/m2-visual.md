# M2 — Primera superficie radial

Estado: preview verificado en Windows; M2 sigue en curso. No recepción Explorer ni conversión en este modo.

Componente WinUI con siete sectores de ejemplo, centro y huecos sin acción, selección separada de confirmación, flechas/Home/End/Enter/Space/Escape y botones con nombres accesibles. Ventana ajustable con temas System/Light/Dark, tamaño320..480, opción Reduce motion y Acrylic administrado por Windows con fallback sólido. No modifica preferencias Windows ni registra teclas globales.

Build04 locked restore: salida0,0advertencias/errores,53tests aprobados (27Core+26integración). Once casos nuevos cubren geometría, huecos, centro/exterior, cancelación, separación hover/confirmación y navegación circular. Tests nativos anteriores usan callbacks directos; no equivalen a Explorer.

Observado en UI real03: clic PNG, Right a JPEG, Enter y Escape actualizan la presentación. Se detectó texto oscuro ilegible y un nombre accesible fijo que ocultaba el mensaje. Build04 corrige tema del host externo, tinte, AutomationId y altura inicial. Revisión04: Light/System y Dark legibles, estado completo visible, clic PNG muestra mensaje real en UIA y pantalla; ReduceMotion=true y cierre normal registrados. Cambios de tamaño320..480 anteriores en ese diario son del propietario, sin atribuirlos a automatización.

Dos fallos iniciales permanecen como evidencia: error XAML y después excepción COM0x80070490 en la suscripción global HighContrastChanged. Esa estrategia quedó cerrada; ThemeSettings por ventana después de Loaded permite arranque y cierre. No afirmar que el fallo sea universal. Una interrupción física Escape detuvo la revisión04 anterior; se continuó solo tras nueva instrucción del propietario.

Originales privados inmutables: m2-visual-build-04.json, m2-visual-core-04.trx, m2-visual-integration-04.trx, m2-visual-final-04.jsonl y m2-visual-result-04.json. Histórico UI03: m2-visual-first-final-01.jsonl y m2-visual-first-ui-result-01.json. Sin datos privados en este informe.

Límites: sin lector de pantalla real, alto contraste del sistema, varias pantallas/DPI físicos, medición de animación ni garantía de apariencia Acrylic efectiva. La ventana es enfocable. Conectar la presentación con el receptor OLE y corroborar el gesto/foco desde Explorer es el siguiente experimento. No se anuncia compatibilidad de formatos por estas etiquetas.
