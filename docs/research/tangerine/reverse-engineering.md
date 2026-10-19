# Metodología y límites RE
Se inspeccionaron fuentes oficiales y una simulación pública. No hubo paquete nativo, decompilación ni ejecución de Tangerine. No se copió código, identificadores internos ni gráficos del fabricante.

S1 y el press kit S3 conducen a ficha oficial Apple S2. En estas superficies no se verificó DMG/ZIP de aplicación descargable. Se revisó la ficha pública sin iniciar sesión, comprar ni eludir Mac App Store. Falta una adquisición legítima por el propietario en Mac; no es una conclusión universal sobre todos los canales del fabricante.
Versión2.0.1 y requisito Apple silicon/macOS13 son metadata pública, no detección de un Mach-O. Formato de paquete, hashes del ejecutable, frameworks, imports, entitlements, símbolos y rutas de eventos permanecen DESCONOCIDOS.

REA MCP6.4.0 respondió binary_session({}), sin target; Ghidra 12.1.4 y JDK21.0.12.1 son disponibilidad declarada comprobada por diagnósticos, no capacidad ejecutada sobre Tangerine. Se conservó respuesta completa privada. No fue necesario ejecutar setup ni modificar configuración. Ayuda CLI real comprobada por wrapper WSL. Node Windows25.8.1 disponible; motor requerido por paquete debe comprobarse antes de cualquier futura instalación, que no se efectuó.

Inventario: Windows Java21.0.7/Python3.12.6; herramientas POSIX file/readelf/strings/7z localizadas en WSL. llvm-objdump/llvm-otool no localizados en PATH comprobado. No se instalaron herramientas sin objetivo.
Windows/WSL comparten LICENSE por hash; no segundo checkout. WSL2 no ejecuta macOS nativo.

Si se aporta copia legítima: preservar procedencia/version/SHA256; inventariar .app/Info.plist/Mach-O/entitlements sin ejecutar; escoger REA por formato, registrar evidencia e inferencias; no traducir pseudocódigo ni reutilizar activos. La especificación funcional propia es la única entrada a implementación.
