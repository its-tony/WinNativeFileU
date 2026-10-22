# M5 — Candidato local 0.1.0-beta.1

Fecha: 2026-10-21, America/Guatemala. **Preparación local verificada en el alcance descrito; Public Beta sin publicar y sin aprobación de distribución pública.** Continúa el [flujo de conversión aceptado](m5-conversion-flow.md). [Resumen revisado](../verification/m5-local-candidate.json).

## Summary

Versión 0.1.0-beta.1, assembly/file 0.1.0.0. Payload autocontenido .NET 10.0.13/Windows App SDK 2.5.1, ZIP y setup x64 por usuario, sin firma. El candidato se construyó desde inputs comprometidos `1e4ec281f6e1090481d39c8be793a4c29a42a6d9`: sourceChangesPresent=false y repositoryChangesPresent=true por tracking/harness separado. No se promete reproducción byte por byte. Artefactos privados en `artifacts/WinNativeFileU-0.1.0-beta.1-candidate`; no push/tag/release ni startup activado.

## Reverse Engineering Findings

Sin nuevos binarios propietarios ni recursos copiados. Se inspeccionaron ayuda/capacidades del compilador Inno 7.1.0 oficial y dependencias propias del publish. Las atribuciones binarias se basan en SHA256 contra NuGet/MSIX x64 legítimos; ejecución Windows y documentación quedan distinguidas. La proyección SDK implícita se identificó desde el deps real, superando el inventario preliminar de lockfiles.

## Architecture Decisions

[ADR0012](../decisions/0012-local-beta-packaging.md): carpeta self-contained, sin single-file/trimming/ReadyToRun; manifiesto, avisos, manual y atribución incluidos. Inno per-user/lowest/Windows11/x64, directorio vacío y sin instalación previa; actualización requiere desinstalar primero. No descarga de runtime, autolanzamiento, asociaciones ni startup automático. Mutex de presencia de la app detiene instalación/desinstalación mientras está viva, sin matarla. No es un lock transaccional contra carreras posteriores o procesos de otra sesión.

Dos identidades: la de test permite negativas y ejecución aislada sin acceso Inicio; la normal también fue probada en destino privado con su acceso Inicio real. Desinstalador estándar elimina archivos conocidos, conserva ajenos/perfiles/resultados; solo elimina Run exactamente coincidente con su propio comando. Ese último caso real no se activó ni ensayó.

## Files Changed

- Version.props, import común y App: versión/runtime fijado y mutex de presencia.
- Installer y scripts: procedencia del compilador, publish, avisos, atribución, integridad e instalación/desinstalación aisladas.
- CI: tray InteractiveShell separado; workflow manual prepara artefactos para revisión sin publicar.
- Manual de usuario, procedimiento de release, revisión de distribución, ADR, estados y evidencia resumida.

## Tests

Build locked Windows completo: **131 aprobadas (47 Core, 54 integración, 30 conversores), cero advertencias/errores**. Fuente con versión/mutex: m5-package-build-02.json y TRX m5-package-*-02.trx. Rama Headless ejercida localmente: **130 (47/53/30), cero advertencias/errores**, excluyendo solo el tray InteractiveShell; m5-headless-build-01.json y tres TRX inmutables. No se ejecutó GitHub Actions remotamente. La clasificación posterior del test no cambia su cuerpo; ningún C# de producto cambió después de la suite completa.

Publish final salida 0; **553 entradas más files.json**, avisos de 16 pares paquete/versión más Inno. **510 binarios/recursos: 504 coincidencias SHA exactas y seis outputs propios/generados, cero desconocidos**. Licencias originales conservadas; atribución técnica no equivale a aprobación legal. NuGet público del 21 de octubre: siete proyectos/salida0/sin entradas de vulnerabilidades; no cubre exhaustivamente packs implícitos, SO ni todo advisory. [Revisión de distribución](../distribution-review.md).

ZIP final extraído y verificado. Alteración y archivo no listado detectados en copia desechable; fuente y ZIP intactos. Evidencia m5-final-integrity-test-01.json. Ambos setups compilaron desde el mismo payload; SHA256 final:

| Artefacto | SHA256 |
|---|---|
| WinNativeFileU-0.1.0-beta.1-win-x64.zip | ec2a9bf53e78f2917d5014bc624cb1c4b14374a809630de0f12ff37d31d6cb76 |
| WinNativeFileU-0.1.0-beta.1-win-x64-setup.exe | a9d320c13da736f779acb536b6ac33ffa27da2b6ae666238ff3d987e62e103fc |

Identidad test instalada en carpeta privada con espacio/Unicode: salida0/manifiesto exacto; coreclr.dll y Microsoft.UI.Xaml.dll realmente cargadas de esa instalación. Directorio ocupado rechazado con salida7 y sentinel intacto; instalación/desinstalación con app viva rechazadas1/1, sin cerrar el proceso ni crear destino nuevo. El propietario cerró con Exit desde bandeja: diario confirma comando3, ambos Revoke0, cinco limpiezas true/ResidentClosed/proceso ausente; snapshots inmutables antes de hashes.

Control UI del residente instalado falló dos veces: captura negra y `failed to activate captured window`. Estrategia cerrada; no se infiere fallo del conversor. Condición nueva documentada: residente cerrado y banco instalado nuevo, lanzamiento Normal explícito en lugar de Hidden. Captura legible; clics reales producen BMP→PNG→BMP y OutputExists visible. Parser Python independiente de PNG/BMP confirma los seis colores RGB3×2 en ambos resultados; dos originales intactos/dos outputs/sin parciales. AltF4 normal registra Closed y proceso ausente. No se demuestra que Hidden causara el fallo ni se atribuye un nuevo gesto picker/radial a este banco.

Desinstalación test después del cierre: salida0, archivos conocidos/key retirados; archivo ajeno añadido, fixture de perfil externo y Run conservados. Normal setup separado: registro/acceso Inicio ausentes antes; instalación0/manifiesto exacto/link con destino y working directory correctos; desinstalación0 retira link/key/archivos conocidos y conserva ajeno/perfil/Run. App no autolanzada y startup nunca activado. Evidencia combinada m5-install-result-01.json, m5-installed-conversion-result-01.json; originales y salidas de conversión fuera de instalación permanecen.

Fallos de build preservados: pin global de RuntimeFrameworkVersion pidió erróneamente Windows SDK.NET.Ref10.0.13 (NU1505/NETSDK1112); se corrigió solo el framework .NET. Primer publish con RID global modificó grafos locked de librerías (NU1004); se quitó -r global y se usó RID propio del App. Stagings fallidos/desarrollo se conservan y no son el candidato final. No se repitió ninguna estrategia cerrada sin cambio/evidencia explícita.

## Limitations

Sin firma, reputación SmartScreen, máquina limpia, ruta normal predeterminada o wizard interactivo. Normal identity/shortcut sí probados en destino privado. Remote CI, logon/Explorer restart, eliminación real de Run coincidente, cierre ocupado/end-session y cancelación del picker sin ensayo. Una pantalla DPI144; lector, alto contraste y otras pantallas pendientes. Variante/corpus de imágenes limitado; sin garantías frente a carreras hostiles o pérdida de energía. Minimización de dependencias, rendimiento y retención de diarios pendientes. Se mantienen los límites y fallos de los informes anteriores.

## Git

Rama codex/m5-conversion-flow. c27a72b versión/runtime/guard; f20e0be empaquetado/installer; 1e4ec28 CI/headless; e956100 harness de instalación/cierre/shortcut verificado. Documentación y evidencia resumida en commit separado, sin reconstruir ni alterar estos artefactos. Licencia existente conservada; evidencia/binarios fuera de Git. Sin push/publicación.

## Next Milestone

Preparación local de M5 entregada para revisión. El objetivo Public Beta permanece pendiente de decisión de distribución y validaciones declaradas; no se marca publicación como realizada. [Procedimiento concreto](../RELEASING.md). No iniciar otro milestone automáticamente.
