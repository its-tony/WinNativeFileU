# Cierre del Milestone 1

Fecha: 2026-10-19, America/Guatemala. Resultado: **viabilidad Windows/Explorer verificada en el alcance de fixtures locales, perfil Letters y una pantalla DPI144**. El propietario autorizó continuar con M2 después de este cierre. No es certificación general de hardware, fuentes de arrastre ni conversión.

## Evidencia de aceptación

El modo RequireHeldActivation bloqueó el prearmado y rechazó hotkeys con botón suelto. Dos gestos humanos nuevos registraron Ctrl+Alt+D con botón abajo, Armed origin hotkey con la misma muestra y foco antes=después. Explorer entró inmediatamente con ambos archivos y terminó en DropAccepted count2/COPY1 a15:44:24 y15:44:33. El rectángulo quedó dentro del área útil a DPI144. Nombres/cantidad/tamaños/SHA256 siguieron intactos después del cierre.

El propietario confirmó «SI, funciona re bien» al procedimiento de soltar las tres teclas manteniendo el ratón y después soltar los archivos. Primera entrada/Over keys41 corresponde a botón+Ctrl+Alt todavía pulsados; Drop keys0 los muestra liberados al entregar. El receptor solo registra el primer Over, por lo que no se capturó el estado intermedio keys1. La aceptación combina esa confirmación humana con apertura impedida sin botón retenido, callbacks Explorer reales y Drop liberado; no se afirma que Drop0 por sí solo pruebe el instante previo de key-up. Se explicita esta precisión respecto al criterio de instrumentación original K1G.

Escape y limpieza propia, salida/reentrada, rechazo funcional durante Tracking y salida sobre fondo del host sin recepción quedaron corroborados en los ensayos anteriores. Un rechazo adicional del run final registró Ctrl+Alt+X/Tracking/botón abajo → latch → Leave/botón suelto/sobre receptor, sin aceptación en esa sesión. Cierre normal final: RevokeDragDrop hr0 a15:46:42, ventana ausente y check posterior correcto.

## Validación automática

Build Windows real locked restore: salida0,0advertencias/errores,42pruebas aprobadas.16Core,16receptor oculto con STA/HWND/OLE/HGLOBAL reales y productores/pistas sintéticos,1perfil inválido y9geometría. Los tests no son un origen Explorer ni prueba física de otras escalas.

Topología real enumerada: una pantalla, DPI144, botones no intercambiados. Escalas96/192/288, coordenadas negativas/extremas y área pequeña se cubren numéricamente. No se modificaron ajustes globales para simular otra configuración.

## Límites conservados

- Receptor restringido a fixtures físicos locales; no escribe, mueve, convierte ni sobrescribe originales.
- F8 físico sigue sin causa establecida; Letters permite el flujo probado mediante API pública sin hooks, polling o inyección.
- Soltar fuera puede dejar el cuadro visible hasta rechazo/cierre explícito.
- No hubo DropRejected físico en los rechazos observados; no se infiere una regla de despacho de OLE. El resultado funcional sí se corrobora; Drop/NONE tiene además cobertura sintética.
- No se identifica retroactivamente la causa de las muestras falsas de botón de ejecuciones anteriores ni se las promueve a pruebas positivas.

Originales privados: m1-completion-final-01.jsonl, m1-completion-result-01.json, m1-final-build-01.json y TRX inmutables; el resultado correlaciona hashes de Escape/reentrada/rechazo anteriores. Resumen revisado: [m1-resume.json](../verification/m1-resume.json). Fuente del último prototipo en5d9c9d9; los informes previos son históricos.

M1 permite continuar con M2 radial UI según la instrucción del propietario. Conversión real, residencia, startup y distribución permanecen en hitos posteriores.
