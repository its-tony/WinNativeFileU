# Hipótesis
| ID | Estado | Proposición y límite |
|---|---|---|
| H-R0 | DOCUMENTADO | El flujo tap/release/drop está descrito en la web actual; App Store conserva texto de mantener Shift. Divergencia sin resolver en app nativa. |
| H-OLE | HYPOTHESIS para aparición durante drag | Un HWND propio recibió un archivo Explorer con target prearmado; aparición a mitad de drag aún no demostrada. |
| H-KEY | HYPOTHESIS | Una combinación configurable mediante RegisterHotKey puede activar ese HWND durante el arrastre sin robar foco. No prueba una tecla modificadora aislada. |
| H-FALLBACK | HYPOTHESIS | Armar el destino antes de iniciar arrastre reduce la dependencia de captura global de teclas. |
| H-NATIVE | BLOQUEADO | Análisis estático/dinámico de Tangerine nativo: sin paquete legítimo ni Mac. No repetir búsqueda de DMG sin fuente oficial nueva. |
| H-SDK | VERIFICADO parcialmente | SDK 10.0.402 instalado y ejecuta --info; no acredita compilación WinUI. |
| H-BUILD | VERIFICADO en Windows para M0 | build-03 locked restore, App/Core/Tests compilados y4/4 tests;0 advertencias/0 errores. No ejecución manual WinUI ni integración Explorer. |
| H-M1-BUILD | VERIFICADO para código compilado | m1-build-01: App/integración nativa/Core/Tests,16 tests aprobados. No ejecución real de OLE todavía. |
| H-M1-F8 | BLOQUEADO para validación en esta sesión | F8 funciona antes del drag; no demostrada entrega durante drag. API no expone tecla en mouse hold, bloqueó destino de otro proceso y después Escape físico detuvo Computer Use. |
| H-M1-SAFE | VERIFICADO automáticamente solo política | Máscaras nunca eligen MOVE; cancelación rechaza; límites léxicos de fixtures pasan tests. Reparse/archivos reales y cancelación OLE pendientes. |
| H-M1-START | VERIFICADO en Windows | Arranque corregido: STA/OleInitialize0/RegisterDragDrop0/F8F9true/Ready. Primer fallo IntPtr conservado. |
| H-M1-ARM | VERIFIED IN WINDOWS PROTOTYPE, parcial | F8 desde Explorer mostró target con foreground antes=después, DPI144. No fue durante drag. |
| H-M1-AUTO | BLOQUEADO para automatización cruzada | sky.drag rechaza endpoint en proceso distinto antes de eventos. No prueba incompatibilidad Explorer/OLE; necesita gesto manual. |
| H-M1-RECEIVE | VERIFIED IN WINDOWS PROTOTYPE, parcial | Enter/Over/DropAccepted COPY con un fixture Unicode desde Explorer; SHA256/nombres/tamaños intactos. Destino previamente armado, no tecla durante drag. |
| H-M1-CANCEL | HYPOTHESIS | Segundo Enter/Over terminó con Leave sin Drop; puede ser salida o cancelación del origen. No se probó cuál ni resultado global Escape. |
| H-M1-CLOSE | VERIFIED IN WINDOWS PROTOTYPE | Snapshot final nuevo contiene RevokeDragDrop hr0 y la ventana ya no figura en listado; no es todavía auditoría de todos los recursos/hotkeys. |
| H-M1-REENTRY | VERIFICADO en receptor oculto; HYPOTHESIS física | Con botón inyectado abajo sin Escape mantiene AwaitingData y acepta reentrada. Requiere gesto real Explorer; soltar fuera puede dejar receptor visible hasta F9. |
| H-M1-NATIVE | VERIFICADO en Windows, receptor oculto |10escenarios con HWND/STA/Register/Revoke y HGLOBAL nativos pasaron. Invocación directa; productor sintético, no Explorer/DoDragDrop. |
| H-M1-DIAG | VERIFIED IN WINDOWS PROTOTYPE, parcial |HotkeyReceived/ArmIgnored reflejan F8 real con LeftButtonDown=false. F9 y texto Receiver closed comprobados tras corregir estado obsoleto; no durante drag. |
