# ADR 0001 — Base WinUI 3
Estado: aceptada para M0; ejecución/overlay pendientes de evidencia.
Contexto: Windows11 x64; separar UI de dominio, sin motores de conversión en esta etapa.
Alternativas: WinUI3 para Fluent/APIs modernas; WPF para overlay si M1 demuestra límites; Win32 puro como host reducido.
Decisión: .NET10 LTS, WinUI3/Windows App SDK 2.5.1 estable según fuente oficial al consultar, unpackaged x64. Fijar SDK 10.0.402 y paquetes exactos. Ventana de diagnóstico mínima; sin Acrylic, radial, hooks ni red en runtime.
Razón: cumplir plataforma preferida y probar primero toolchain. Core independiente con modelo mínimo de recepción; tests fuera UI. No crear proyectos vacíos WindowsIntegration/Converters.
Consecuencias: requiere herramientas y runtime Windows adecuados; dependencias no equivalen a ejecutable único. Distribución/instalador y cierre residente son trabajo futuro. WSL solo investigación, no validación Explorer/WinUI.
Riesgos: XAML compiler puede requerir componentes de VS; SDK NuGet/runtime pueden no estar disponibles. Fallo se registra sin degradar silenciosamente a WPF. M1 decide estrategia overlay con evidencia real.
