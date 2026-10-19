# Bitácora
Sesión: 2026-10-18 America/Guatemala (UTC-06:00). Los archivos originales están en evidence/private, excluidos de Git.

## EXP-R0-01 — Workspace y entorno
Objetivo: identificar el destino y herramientas sin alterar el laboratorio padre.
Procedimientos: Get-ChildItem -Force; git status --short; git log -3 --oneline; git remote -v; dotnet --info; node --version; python --version; java -version; Get-Command de herramientas solicitadas; lectura del registro Windows y SDK bin.
Resultado: repositorio inicial c8e2451 limpio con LICENSE/.gitattributes; remoto ya existente, sin operación remota. Windows registro build 26200/25H2 (ProductName legado indica Windows 10 Home); .NET host 10.0.13 sin SDK, Node25.8.1, Python3.12.6, Java21.0.7. Windows SDK bin hasta22000 observado, sin inferir instalación completa.
Evidencia: evidence/private/initial-environment.json y transcripción de herramientas para inventario complementario.
Limitación: orden combinada salida0 no significa que WSL funcionó: wsl --list --quiet reportó E_ACCESSDENIED. No se atribuye a ausencia de WSL.

## EXP-R0-02 — WSL y REA
REA binary_session({}) respondió, sin target abierto:6.4.0,170tools, Ghidra 12.1.4/JDK21.0.12.1 disponibles según diagnósticos. No decompilación.
Primer comando WSL fuera sandbox usó sh -lc y falló1 por perfil swiftly/env.sh (Bad substitution). Se cambió a bash --noprofile --norc, evidencia nueva de causa de perfil.
Procedimiento válido: wsl -d Ubuntu --exec bash --noprofile --norc -c con uname -sr, command -v de herramientas, sha256sum de LICENSE compartida y ayuda del wrapper REA instalado. Ruta personal del wrapper omitida en resumen público; comando exacto en transcripción y evidence/private/wsl-procedure.json.
Salida0: Linux6.6.87.2 WSL2; python3/java/file/readelf/strings/7z en PATH, Node/llvm-objdump/llvm-otool no localizados en ese PATH. REA wrapper responde ayuda. Hash LICENSE coincide con Windows: bc83f561c857090f9cf0b8aca33afe369b72986ce13095dbebe4f5e751135886. Un solo checkout compartido.
Evidencia: evidence/private/rea-session.json; transcripción para WSL/hashes. No reinstalación ni configuración global.
Siguiente: sin binario legítimo de Tangerine no procede Ghidra.

## EXP-R0-03 — Fuentes y demo pública
Herramientas: web lector y navegador IAB; versiones del servicio no expuestas.
Procedimiento: abrir sitio oficial, press kit y ficha Apple enlazada. En demo, landscape.jpg -> Return -> Right -> Escape.
Resultado OBSERVED (web simulada): aparece menú PNG/WebP/HEIC/TIFF/AVIF/BMP/PDF; Right cambia indicación a WebP; Escape quita menú y devuelve foco al archivo. No se confirmó operación ni se cargaron archivos del usuario. Código no aplica.
Evidencia: evidence/private/web-observation.json; docs/research/tangerine/sources.md. La web declara simulación; no se observó Finder ni arrastre nativo.
Distribución pública remite a Mac App Store; no paquete directo verificado. No se intentó evadir cuenta/compra/acceso.
Siguiente: especificación independiente y evaluación Windows.

Entrega R0: siete documentos en docs/research/tangerine, especificación independiente y dos ADRs. Tarea R0 resuelta con alcance de investigación pública; no desbloquea análisis nativo ni acredita Windows prototype. Inspección visual adicional de la simulación mostró siete sectores alrededor de un centro, selección naranja y etiquetas contrastantes; sin copiar recursos ni medir animación/latencia. Screenshot solo en transcripción de herramienta, no archivo público.

## EXP-M0-01 — SDK local
Primer fetch metadata en sandbox falló1: host desconocido. Mismo origen fuera sandbox funcionó0: metadata oficial declara SDK 10.0.402. Cambio de permisos resuelve DNS sin cambiar validación TLS.
Instalación exacta y resultado final se registran mediante tracker SDK en evidence/private/sdk-install-01.json; SHA512/origen en sdk-provenance.json al completarse. No instalar globalmente ni suponer compilación por presencia del runtime.

Resultado final SDK: salida0, SDK 10.0.402/MSBuild18.9.15; SHA512 coincidente con metadata Microsoft. Solo .tools local. Tarea SDK VERIFICADO por evidencia de instalación real.

## EXP-M0-02 — Primera compilación
Comando: python scripts/task_tracker.py run BUILD --evidence evidence/private/build-01.json --note 'Restore/build Windows WinUI shell and run domain tests; review individual results' --timeout 300 --command pwsh -NoProfile -File scripts/build.ps1
Resultado: salida1, MSB4126 Debug|x64 no declarado en solución slnx; no compilación ni tests ejecutados. Causa identificada: faltaba Configurations/Platform x64. Se añadió esa plataforma explícita antes del siguiente intento; no se repite sin cambio.
Evidencia: evidence/private/build-01.json. Siguiente: restaurar/compilar con configuración corregida.

## EXP-M0-03 — Compilación Windows válida, invocación de tests incorrecta
Comando: mismo tracker BUILD, evidence/private/build-02.json, script/build.ps1 corregido en x64 de solución.
Resultado: restore0 y compilación Windows real correcta, 0 advertencias/0 errores; App/Core/Tests producidos. Orden final test falló1: solicitó bin/x64/Release cuando solución había compilado Core/Tests en bin/Release (AnyCPU). Ninguna prueba ejecutada en este intento. No se confunde fallo del runner con fallo de compilación.
Causa nueva: la invocación directa forzaba Platform=x64 mientras slnx mapea proyectos de dominio a AnyCPU. Se quitó ese parámetro de dotnet test. Tracker conservador cerró BUILD tras dos fallos de comando, de causas distintas. Reapertura explícita con scripts/build.ps1 modificado como evidencia nueva; no repetir sin corrección. Siguiente: build con locked restore y tests corregidos.

## EXP-M0-REVIEW-01 — Revisión de rutas
Comando tracker REVIEW ejecuta python scripts/review-repository.py; salida0. Revisa rutas candidatas, siete documentos requeridos y git diff --check. Original evidence/private/review-01.json. Alcance: no escáner universal de secretos ni sustituto de revisión manual. Estado/documentación pendiente de revisión final del contenido.

## EXP-M0-04 — Verificación final de build y dominio
Comando: python scripts/task_tracker.py run BUILD --evidence evidence/private/build-03.json --note 'Locked restore and corrected AnyCPU test invocation after documented path mismatch' --timeout 180 --command pwsh -NoProfile -File scripts/build.ps1 -LockedRestore.
Resultado: salida0; locked restore correcto; App/Core/Tests compilados Windows x64/AnyCPU según proyecto;0 advertencias/0 errores. TRX:4 totales,4 ejecutadas,4 aprobadas,0 fallidas/omitidas. Modelo probado: activación no entrega archivos, reset rechaza drop tardío en misma sesión, snapshot independiente/inmutable y entrada inválida no confirma selección. No se hicieron operaciones de archivo ni se probó concurrencia/adaptador OLE.
Evidencia: evidence/private/build-03.json; original de TRX en artifacts/test-results/core.trx copiado a evidence/private/core-tests-03.trx. Tareas BUILD y TEST VERIFICADO en alcance M0. No es VERIFIED IN WINDOWS PROTOTYPE para el gesto: ni ventana ejecutada manualmente, ni Explorer/hotkey/drop probados.
Siguiente: revisión de contenidos y commits locales; no avanzar a M1.

## EXP-M0-REVIEW-02 — Revisión final y Git local
Fecha:2026-10-18 America/Guatemala. Revisión manual de fuente C#/XAML/scripts, docs y lock files; script de rutas con documentos finales salida0 en evidence/private/review-02.json. Patrón acotado de claves API/GitHub/llaves privadas:0coincidencias en candidatos; no garantía universal. Se retiró ruta personal WSL del resumen público y se preservó procedimiento derivado de transcripción en privado. git check-ignore confirma SDK/evidencia/bin/resultados excluidos; LICENSE y .gitattributes sin diff.
Commit local de investigación e519edd,11archivos revisados; no binarios propietarios ni originales privados. Fundación se prepara aparte. Sin push, release ni modificación del remoto. Tarea REVIEW VERIFICADO para revisión local; CI/remoto y pruebas manuales siguen pendientes.
