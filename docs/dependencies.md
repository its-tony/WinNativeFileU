# Dependencies and distribution boundary
No release is produced in R0/M0. Existing root MIT license covers original project code, not third-party components.

Direct references: Microsoft.WindowsAppSDK 2.5.1 for the App; Microsoft.NET.Test.Sdk 18.11.0, xunit 2.9.3 and xunit.runner.visualstudio 4.0.1 for tests. Versions and transitive content hashes are captured in packages.lock.json. NuGet.org is the only configured package source.

The Windows App SDK metapackage brings multiple Microsoft modules (including modules not called by this shell). No AI/conversion engine functionality is implemented by referencing them. Review whether narrower packages are suitable before distribution; do not claim minimal final size or memory yet.

The SDK ZIP has official Microsoft URL and SHA512 provenance in ignored evidence. It is not redistributed. Private source research and tools are never bundled with the app.

M3 Converters calls the operating system's Windows.Graphics.Imaging PNG/BMP codecs through the existing Windows SDK projection. It adds no external image package or downloaded executable. Its test project reuses the existing test package versions; App now references Converters and lock graphs were regenerated and verified with locked restore. This does not establish support for every Windows codec input or replace a deployment/license review.

M4 uses public user32/shell32 functions, .NET registry, JSON and named mutex APIs. No new package, tray framework, executable or installer was added. Tray and hotkey integration tests require an interactive Windows shell; this is an environment requirement, not a new redistributed component. Real sign-in startup and remote CI have not been verified.

M5 local candidate now bundles .NET10.0.13 and Windows App SDK runtime files, plus the implicit Windows SDK.NET.Ref10.0.22000.57 projection. The final payload has original notices for16package/version pairs plus Inno7.1.0, with exact binary/resource attribution (504 upstream hashes plus6original/generated outputs,0unknown). SDK/test/build tools are excluded. The public NuGet advisory query was refreshed2026-10-21 without entries for its reported seven-project graph; implicit runtimepacks/OS components are not comprehensively covered. See [distribution review](distribution-review.md) for terms and scope. Dependency minimization and clean-machine deployment remain pending. No public-release, legal-approval or security-guarantee claim is made. FFmpeg, external image libraries and PDF engines have not been added.
