# Hipótesis
| ID | Estado | Proposición y límite |
|---|---|---|
| H-R0 | DOCUMENTADO | El flujo tap/release/drop está descrito en la web actual; App Store conserva texto de mantener Shift. Divergencia sin resolver en app nativa. |
| H-OLE | HYPOTHESIS | Un HWND propio registrado como destino OLE puede recibir un arrastre ya iniciado al aparecer cerca del cursor; debe probarse en M1 con Explorer real. |
| H-KEY | HYPOTHESIS | Una combinación configurable mediante RegisterHotKey puede activar ese HWND durante el arrastre sin robar foco. No prueba una tecla modificadora aislada. |
| H-FALLBACK | HYPOTHESIS | Armar el destino antes de iniciar arrastre reduce la dependencia de captura global de teclas. |
| H-NATIVE | BLOQUEADO | Análisis estático/dinámico de Tangerine nativo: sin paquete legítimo ni Mac. No repetir búsqueda de DMG sin fuente oficial nueva. |
| H-SDK | VERIFICADO parcialmente | SDK 10.0.402 instalado y ejecuta --info; no acredita compilación WinUI. |
| H-BUILD | VERIFICADO en Windows para M0 | build-03 locked restore, App/Core/Tests compilados y4/4 tests;0 advertencias/0 errores. No ejecución manual WinUI ni integración Explorer. |
