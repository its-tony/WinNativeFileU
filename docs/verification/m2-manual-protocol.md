# M2 — Protocolo radial acotado

Usar únicamente fixtures nuevos del launcher. No convertir ni procesar archivos personales. El diario distingue activación, selección radial, recepción y fin de sesión. La automatización disponible no puede realizar el arrastre entre ventanas; no repetir ese método bloqueado.

Preparación después de build Windows:

```powershell
pwsh -NoProfile -File scripts/start-prototype.ps1 -ShortcutProfile Letters -Radial
```

Comprobar PresentationReady/RegisterDragDrop0/Ready, ventana real y conservación de foco con control idle. Después:

1. Abrir fixtures desde el host. Con receptor cerrado, comenzar arrastre de ambos y pulsar/soltar Ctrl+Alt+D manteniendo ratón. Mover a PNG y soltar. Correlacionar Armed/foco/DPI, datos count2, RadialSelection PNG y DropAccepted selectedFormat PNG/COPY1/previewOnly.
2. Desde otro arrastre abrir y entrar en un sector, luego mover al centro y soltar. Exigir ausencia de aceptación. Explorer puede concluir un efecto NONE mediante Leave; no inventar DropRejected si no existe.
3. Abrir durante otro arrastre, entrar/salir/reentrar y soltar en otro sector. Verificar etiqueta final y ausencia de reutilización obsoleta.
4. Escape: desaparecen miniatura y receptor. Ctrl+Alt+X durante Tracking: no aceptación posterior; registrar si llega Drop o Leave. Salida exterior: conservar la limitación M1 de cierre explícito cuando el origen termina fuera.
5. Verificar nombres/cantidad/tamaños/SHA256 mediante check-prototype-files.ps1 -RunFile con el run.json exacto. Cerrar normal y comprobar Revoke0/ausencia de ventana, disponiendo el island antes del HWND.

La primera ejecución diagnóstica permite prearmar; no atribuirle el gate. Para una prueba posterior que exija activación retenida añadir -RequireHeldActivation; el control idle debe producir ActivationDeclined y el botón Arm estar deshabilitado. Conservar límites de muestras de teclas de M1.

Documentar solo las configuraciones físicas disponibles. Tests de ubicación en otras escalas no validan equipos reales. Preview enfocable cubre teclado/nombres/temas por separado; no afirmar lector de pantalla o alto contraste físico sin ensayo. M2 permanece abierto hasta revisar aceptación y límites, sin iniciar M3.
