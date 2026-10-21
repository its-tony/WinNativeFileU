# Dependencies and distribution boundary
No release is produced in R0/M0. Existing root MIT license covers original project code, not third-party components.

Direct references: Microsoft.WindowsAppSDK 2.5.1 for the App; Microsoft.NET.Test.Sdk 18.11.0, xunit 2.9.3 and xunit.runner.visualstudio 4.0.1 for tests. Versions and transitive content hashes are captured in packages.lock.json. NuGet.org is the only configured package source.

The Windows App SDK metapackage brings multiple Microsoft modules (including modules not called by this shell). No AI/conversion engine functionality is implemented by referencing them. Review whether narrower packages are suitable before distribution; do not claim minimal final size or memory yet.

The SDK ZIP has official Microsoft URL and SHA512 provenance in ignored evidence. It is not redistributed. Private source research and tools are never bundled with the app.

M3 Converters calls the operating system's Windows.Graphics.Imaging PNG/BMP codecs through the existing Windows SDK projection. It adds no external image package or downloaded executable. Its test project reuses the existing test package versions; App now references Converters and lock graphs were regenerated and verified with locked restore. This does not establish support for every Windows codec input or replace a deployment/license review.

Before any beta distribution: inspect resolved package license expressions/files and native third-party notices, include required notices, review vulnerabilities, remove unneeded dependencies and validate runtime/deployment prerequisites. No claim of completed distribution/license audit is made here. FFmpeg, external image libraries and PDF engines have not been added.
