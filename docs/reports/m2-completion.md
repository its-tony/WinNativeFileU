# M2 — Aceptación acotada del prototipo radial

Fecha: 2026-10-20, America/Guatemala. **M2 completado para el prototipo Windows descrito aquí.** M3 requiere nueva instrucción del propietario. Sin motor de conversión, push ni release.

## Summary

Radial WinUI original con siete sectores de ejemplo, ventana enfocable de previsualización y presentación circular sobre el receptor OLE de fixtures. El propietario confirmó funcionamiento tras corregir la entrada nativa. Se conservan los fallos y los límites; no es una declaración de preparación para distribución ni de compatibilidad real de los formatos mostrados.

| Tarea M2 | Implementado y verificado | Límite de aceptación |
|---|---|---|
| Radial layout | Geometría pura, centro/huecos sin acción, círculo real de 400 DIP a DPI 144 | Siete etiquetas visuales, sin motores |
| Acrylic | SystemBackdrop de WinUI e island, soporte observado y fallback sólido | Apariencia efectiva depende de Windows; blur no medido |
| Selección | Cambios reales por OLE, recepción de lotes y recomputación al Drop | Recepción física final PNG; no JPEG |
| Animaciones | Escala de 120 ms, AnimationsEnabled y Reduce motion; cambio de opción observado | Duración/renderizado no cuantificados; sin benchmark |
| Temas | System y selector Light/Dark en preview; legibilidad revisada | El radial OLE sigue System; selector local en preview |
| Accesibilidad | Botones nativos con nombres, texto UIA real y flechas/Enter/Escape observados en preview enfocable | Sin lector de pantalla real; overlay de arrastre permanece pasivo |
| Tamaño/posición | Preview de 320–480 DIP; radial de 400 DIP cerca del cursor y dentro del área útil | Una pantalla física DPI 144; otras escalas solo pruebas numéricas |

## Reverse Engineering Findings

Sin nuevos artefactos propietarios ni nuevas afirmaciones sobre Tangerine. La investigación R0 y su separación de la implementación permanecen en [research](../research/tangerine/overview.md). Hallazgo propio OBSERVED: registrar solo el padre de la presentación WinUI dio 0 callbacks OLE en el ensayo fallido. La entrada nativa propia recuperó recepción Explorer; el mecanismo interno exacto del fallo inicial no fue trazado.

## Architecture Decisions

[ADR0007](../decisions/0007-m2-radial-presentation.md) separa preview enfocable de receptor. [ADR0008](../decisions/0008-m2-native-radial-bridge.md) conserva HWND/STA/CF_HDROP/fixtures en WindowsIntegration y WinUI en App. Un hijo nativo propio registrado OLE se sitúa sobre el island con alpha 1; no se modifica el registro/procedimiento del framework. Se revoca antes de destruirlo. Región circular con propiedad transferida al sistema cuando SetWindowRgn tiene éxito.

No hooks, inyección, captura arbitraria de teclas, cambios Windows, operaciones sobre contenidos ni red de ejecución. Solo COPY/NONE y fixtures locales; originales permanecen intactos.

## Files Changed

- Core: RadialMenuState y pruebas de geometría, selección, confirmación/cancelación y navegación.
- App: RadialMenuControl, RadialPreviewWindow, RadialOverlayPresentation y selección de modo/títulos en el host existente.
- WindowsIntegration: interfaz de presentación, hijo de entrada OLE, transformación de coordenadas, región circular, ubicación y diagnósticos de selección/hit-test/cierre.
- Harness: launcher de preview y opciones Radial/RequireHeldActivation del prototipo.
- Documentación: decisiones, informes, protocolos y resúmenes revisados; originales privados ignorados.

## Tests

Última build de la fuente 96fb9d7: `pwsh -NoProfile -File scripts/build.ps1 -LockedRestore`, salida 0, 0 advertencias/errores, **60 aprobadas: 27 Core + 33 integración**. Original m2-input-build-01.json y TRX inmutables m2-input-core-01.trx/m2-input-integration-01.trx. Incluyen 16 casos nativos anteriores, 4 radiales, 1 perfil y 12 de ubicación. Los nativos usan callbacks directos/datos sintéticos; no se presentan como despacho Explorer. El día 20 no se repitió la build: `git diff --quiet 96fb9d7 -- src scripts tests` salió 0; solo cambió documentación.

UI real de preview: Light/System y Dark legibles, clic PNG, Right→JPEG, Enter y Escape, mensaje real accesible y opción ReduceMotion. Evidencias/defectos corregidos en [informe visual](m2-visual.md). Sin prueba de lector de pantalla ni de alto contraste físico.

Pruebas Explorer corregidas: lote PNG/Unicode, salida/reentrada y hashes intactos. Centro con NONE termina mediante Leave sin aceptación; CtrlAltX durante Tracking produce latch/Leave sin aceptación; EscapeDown al Leave cierra el receptor y el propietario confirma desaparición de miniatura. Sin DropRejected físico; selección obsoleta sector-centro está cubierta sintéticamente, sin atribuir al ensayo humano una transición ausente del diario.

Ensayo final: RequireHeldActivation=true, Arm deshabilitado e idle ActivationDeclined. **Cuatro activaciones retenidas** conservan foreground Explorer y reciben datos inmediatamente; cuatro DropAccepted count=2/COPY=1/previewOnly con selección final **PNG**. Una entrega registra keys=0 y las otras tres keys=40 (Ctrl/Alt). El propietario respondió «Si, funciona correcto» al gesto solicitado; se conserva que pedía JPEG y el diario terminó en PNG. JPEG aparece en hover, no se afirma recepción JPEG. El primer Over registra 41; no hay traza del momento intermedio de liberación antes de mouse-up.

Cierre normal final: RevokeInputDragDrop=0 y RevokeDragDrop=0, después ventana ausente; manifiesto post-cierre con nombres/cantidad/tamaños/SHA256 intactos. El estado UIA anterior al cierre decía Received 2 fixture file(s). No es una auditoría universal de recursos o fiabilidad.

Evidencia privada final: m2-completion-final-01.jsonl, m2-completion-result-01.json (verificación de sesiones/build/manifiesto y hashes de evidencia), m2-held-owner-success-01.jsonl y m2-held-ready-result-01.json. Antecedentes en [informe del puente](m2-native-bridge.md). Resumen público en [m2-completion.json](../verification/m2-completion.json).

## Limitations

- Una pantalla física DPI 144; sin lector de pantalla, alto contraste físico ni múltiples pantallas reales.
- Preview enfocable valida teclado/nombres/temas; no constituye un flujo accesible de conversión sobre archivos. Opciones locales sin persistencia.
- Fuente real limitada a fixtures. Labels de formatos son ejemplos; no se convierten ni adjuntan archivos a otro servicio.
- F8 físico sigue sin resolver desde M1; Letters es el perfil probado. Salida exterior puede conservar el receptor hasta rechazo/cierre explícito.
- No garantía de latencia/consumo/fiabilidad ni aprobación estética final. No se ocultan las entregas con Ctrl/Alt retenidos ni el JPEG solicitado/PNG registrado.

## Git

Commits locales: 8c60c9d preview, c32ccb5 verificación visual, 9147737 puente inicial, 3696979 protocolo, 96fb9d7 corrección de entrada y círculo, d9c8408 correlación de recepción/rechazo/cierre. La aceptación se guarda en otro commit documental consultable en el historial. Sin push, release ni cambios de licencia.

## Next Milestone

M3 corresponde al motor offline. **No iniciado ni autorizado por esta aceptación**. Esperar instrucción del propietario; mantener los límites anteriores y usar únicamente especificaciones originales.
