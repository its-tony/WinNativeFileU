# Especificación funcional independiente
Estado: diseño propio R0; no descripción de código propietario.
Objetivo: Windows11 x64, archivos locales, gesto contextual con teclado y drop; conservar originales.
1. Activación solicita mostrar un destino cerca del cursor; no obtiene selección ni formatos.
2. Solo el IDataObject recibido en destino permite evaluar compatibilidad; rechazar virtuales, red y datos desconocidos inicialmente.
3. Liberar la tecla mantiene el menú. Hover solo selecciona; ejecutar requiere Drop válido sobre acción compatible.
4. Cancelar nuestro menú descarta estado. Eso no equivale a cancelar el arrastre de Explorer; M1 debe demostrar finales seguros.
5. Negociar COPY solo si permitido, de lo contrario NONE. Nunca MOVE para convertir; sin escritura en la prueba M1.
6. Operaciones futuras crearán resultados nuevos con colisiones explícitas, temporales y cancelación; no sobrescribir originales.
7. Sin red de ejecución, telemetría de teclas, inyección ni APIs privadas. Startup opcional desactivado inicialmente.
8. Coordenadas/DPI/monitores deben validarse; ofrecer modo enfocable de teclado y semántica UI Automation, alto contraste y movimiento reducido.
Criterios todavía pendientes: recepción Explorer, pulsación durante drag, foco, Escape/abandono, múltiples Unicode, rechazo de formatos/efectos, hashes intactos. No hay números prometidos de latencia/CPU/memoria.
Prioridad: validar gesto antes de radial/Acrylic o motores. Alternativa de baja fricción: armar destino antes de arrastrar. Ninguna de las dos está demostrada por M0.
