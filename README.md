# WinNativeFileU

An original open source Windows 11 file utility, inspired by keyboard-assisted contextual workflows. Files will be processed locally. This project is independent of Tangerine and its developer.

Current scope: **M1 feasibility prototype, partially verified**. A native OLE target hosted by the WinUI app accepts only prepared fixture files. F8 activation before dragging and real Explorer drops were observed, including two trials with both fixtures and a Unicode filename using **Arm target before dragging**. Original names, lengths and SHA256 hashes remained unchanged. The full key-during-drag gesture and safe Escape cancellation remain unverified. M1 is not accepted as complete. Radial UI, conversion, tray and startup are not implemented. See [current status](ESTADO.md), [latest M1 results](docs/reports/m1-resume.md) and [R0/M0 report](docs/reports/r0-m0.md).

## Development

Latest M1 work: [reentry and native receiver tests](docs/reports/m1-resume.md). The final suite has 16 Core and 10 hidden native receiver cases. The receiver now permits reentry after Leave when the mouse button is observed held; F8/F9 diagnostics distinguish received and ignored activation. These tests do not replace the outstanding Explorer gesture validation.

Use the [physical validation protocol](docs/verification/m1-manual-protocol.md) to distinguish button activation from F8 during dragging and to record cancellation, reentry, focus and DPI results.

Windows 11 x64; .NET SDK **10.0.402** (compatible 10.0.4xx patches allowed by global.json). Windows App SDK version is pinned in src/App/WinNativeFileU.App.csproj. Build tools may require Visual Studio/Windows SDK components; dependency restore uses NuGet.org. .NET and App SDK are development/runtime dependencies; REA, Ghidra and WSL are research tools only.

The current checkout is shared with WSL through /mnt/j; do not create a duplicate checkout.

For a project-local SDK, run the reviewed official-source bootstrap:

```powershell
pwsh -NoProfile -File scripts/install-local-sdk.ps1
pwsh -NoProfile -File scripts/build.ps1
```

The installer verifies the official archive SHA512, writes only .tools and private provenance, and refuses to overwrite an SDK directory. The build script prefers .tools/dotnet, restores, builds Release x64 and runs tests; it exits immediately on failure. After lock files have been generated, use -LockedRestore.

With an SDK on PATH, equivalent commands are:

```powershell
dotnet restore WinNativeFileU.slnx -p:Platform=x64 --locked-mode
dotnet build WinNativeFileU.slnx --no-restore -c Release -p:Platform=x64
dotnet test tests/Core.Tests/WinNativeFileU.Core.Tests.csproj --no-build --no-restore -c Release
dotnet test tests/WindowsIntegration.Tests/WinNativeFileU.WindowsIntegration.Tests.csproj --no-build --no-restore -c Release
```

Running the App without prototype arguments opens instructions. To prepare disposable files, a baseline SHA256 manifest and a private journal:

```powershell
pwsh -NoProfile -File scripts/start-prototype.ps1
```

The prototype registers F8/F9 only for its lifetime. Use **Open fixtures**, then F8 to show the receiver near the cursor. Only that run's fixture directory is accepted. F9 rejects/closes; while a drag is inside it latches rejection until Drop/Leave instead of exposing another destination. Escape is handled by the drag source; a logged DragLeave alone cannot prove Escape cancellation. Close the prototype window to revoke OLE and hotkeys.

The receiver records accepted paths; it does not copy, move or convert files. COPY/NONE are negotiated OLE effects. Keep every original journal under evidence/private; do not publish screenshots or identifiers. Validate after a trial with scripts/check-prototype-files.ps1 -RunFile followed by the run.json path printed by the launcher. Do not interpret success of the launcher as readiness; check the window and Ready journal event.

No input hooks or startup changes are made. The unpackaged build includes Windows App SDK components; it still requires .NET 10 x64. Packaging and distribution are future milestones.

## Structure

- src/App: WinUI presentation and lifecycle.
- src/Core: platform-independent domain model; no file I/O.
- src/WindowsIntegration: fixture-only HWND/OLE target, hotkey registration, placement and private diagnostics.
- tests/Core.Tests: safety boundaries for activation, accepted data and reset.
- docs/research/tangerine: evidence, functional analysis and Windows feasibility.
- docs/decisions: architecture decisions.
- evidence/private: ignored original research/build output; reviewed summaries belong in docs.

No conversion engine project is included.

## Research and roadmap

Start with [research overview](docs/research/tangerine/overview.md), [sources](docs/research/tangerine/sources.md), [independent specification](docs/functional-specification.md) and [Windows feasibility](docs/research/tangerine/feasibility.md).

R0 → M0 foundation → M1 actual Windows drag feasibility → M2 radial UI → M3 conversion → M4 background integration → M5 beta. **Do not begin the next milestone without the owner's instruction.** Unit tests and a successful build cannot prove an Explorer gesture.

[Contributing](CONTRIBUTING.md) · [Security](SECURITY.md) · [Code of conduct](CODE_OF_CONDUCT.md) · [MIT license](LICENSE)
