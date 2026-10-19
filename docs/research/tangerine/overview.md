# Tangerine — revisión funcional
Consulta: 2026-10-18 America/Guatemala. Referencias numeradas en sources.md.

| Hallazgo | Clase / confianza | Evidencia | Límite / implicación |
|---|---|---|---|
| Arrastrar, pulsar tecla, soltar sobre rueda contextual | DOCUMENTADO / alta sobre lo anunciado | S1 | No prueba comportamiento interno; especificar interacción original |
| Conversión, herramientas y acciones son categorías distintas, con teclas configurables | DOCUMENTADO / alta | S1 | Windows necesita verificar conflictos antes de asignar teclas |
| Procesamiento local | DOCUMENTADO / alta sobre declaración | S1/S2 | No auditoría de tráfico; producto propio deberá funcionar offline |
| Lotes compatibles, copia junto al original y progreso de trabajo | DOCUMENTADO / media por descripción posiblemente anterior | S2 | Política exacta de colisiones/errores no observada |
| PNG/JPEG/WebP, audio/video, documentos y archivos | DOCUMENTADO / alta sobre familias | S1/S2 | No matriz exhaustiva verificada ni promesa para nuestra versión |
| Entrada con teclado y Escape | OBSERVED en simulación web / alta | EXP-R0-03 | No acredita accesibilidad de app macOS |
| Accesibilidad nativa | DESCONOCIDO | S2 no enumera prestaciones | Nuestro producto requiere UI Automation, teclado, alto contraste y movimiento reducido |

La ficha Apple muestra versión2.0.1 al consultar; no se obtuvo su ejecutable, formato real ni arquitectura por inspección. Compatibilidad declarada: Apple silicon/macOS13+. La investigación se limita a material oficial público y simulación; no se reconstruyó un motor propietario.
