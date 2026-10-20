# WinNativeFileU

An original open source Windows 11 file utility, inspired by keyboard-assisted contextual workflows. Files will be processed locally. This project is independent of Tangerine and its developer.

Current scope: **M2 radial prototype complete within documented limits**. WinUI presentation and real Explorer fixture receipt/selection, reentry and cancellation are verified on one physical DPI144 display. The final held-gated run received four PNG batches; one Drop recorded released keys, three retained Ctrl/Alt. The request named JPEG, while the actual final label was PNG. Originals remained unchanged. See the [M2 acceptance and limits](docs/reports/m2-completion.md), [M1 acceptance](docs/reports/m1-completion.md) and [current status](ESTADO.md). Conversion, tray and startup remain later work; M3 requires a new owner instruction.

## Development

The separate radial visual preview was validated with53tests (27Core,26integration); the latest integrated source passes **60 tests** (27Core,33integration). Light/Dark appearance, selection, keyboard controls and actual accessible status text have been inspected on Windows. Format labels remain visual examples. See the [preview report](docs/reports/m2-visual.md).

```powershell
pwsh -NoProfile -File scripts/start-radial-preview.ps1
```

The optional native radial bridge is verified for local fixtures (60 tests passing, including synthetic selection/drop boundaries):

```powershell
pwsh -NoProfile -File scripts/start-prototype.ps1 -ShortcutProfile Letters -Radial
```

This hosts WinUI inside the native fixture receiver and records the selected example format. A registered native input child sits above the visual island, after the parent-only route failed. Real Explorer PNG delivery, reentry, center rejection, Escape, explicit rejection and held-gate activation are corroborated, with unchanged originals. The surface is circular and400DIP. Both OLE registrations revoke successfully on normal close. See the [M2 report](docs/reports/m2-completion.md) and [protocol](docs/verification/m2-manual-protocol.md). It performs no file conversion. Screen-reader, physical high-contrast and other-display validation remain outside the demonstrated scope.

Latest M1 build passed **42 tests**:16 Core and26 integration (16 hidden native receiver cases,1 profile case and9 placement cases). The held-mouse gate rejects idle/Escape/button prearming and late drops; two real Explorer gated batch trials passed. Original names/counts/lengths/hashes remained unchanged after normal close, RevokeDragDrop hr0 and window absence. F8 physical delivery remains unresolved; Letters is verified. Outside release may keep the receiver visible until explicit rejection, a documented limitation. Historical diagnostics remain in the [M1 continuation report](docs/reports/m1-resume.md).

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

The default FunctionKeys profile registers F8/F9 only for the prototype's lifetime. Physical F8 did not reach the prototype in two owner trials, although an automated F8/F9 control succeeded in the same run; the cause is unresolved. A separate Letters profile is available for testing:

```powershell
pwsh -NoProfile -File scripts/start-prototype.ps1 -ShortcutProfile Letters
```

Letters registers **Ctrl+Alt+D** to show the receiver and **Ctrl+Alt+X** to reject/close. The window and journal display the selected bindings; registration failure disables the prototype. This uses the same public RegisterHotKey API, with no input hooks or polling. Selection happens at launch, with only these two predefined profiles.

To validate activation timing with prearming disabled:

```powershell
pwsh -NoProfile -File scripts/start-prototype.ps1 -ShortcutProfile Letters -RequireHeldActivation
```

This mode opens only when the activation hotkey observes the physical left mouse button held and Escape released. The Arm button is disabled. A held button alone is not proof of an OLE drag; correlate the owner gesture, hotkey/Armed sample and subsequent real Explorer callbacks. It is a diagnostic mode; the default still allows prearming. Startup logs available display work areas/DPI using only the app's hidden HWND. Queue state and message-age hints are logged only for the two registered actions, without arbitrary key capture. See [ADR0006](docs/decisions/0006-m1-held-activation-validation.md).

Use **Open fixtures**, then the displayed activation shortcut to show the receiver near the cursor. Only that run's fixture directory is accepted. The rejection shortcut closes while idle; during a drag inside it latches rejection until Drop/Leave instead of exposing another destination. Escape is handled by the drag source; a logged DragLeave alone cannot prove Escape cancellation. Close the prototype window to revoke OLE and hotkeys. A successful automated shortcut control does not verify physical keyboard delivery or activation during an Explorer drag.

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
The owner authorized completing M1 then M2 on2026-10-19; both prototypes are verified within their reports' scope. M3 has not been started or authorized.

[Contributing](CONTRIBUTING.md) · [Security](SECURITY.md) · [Code of conduct](CODE_OF_CONDUCT.md) · [MIT license](LICENSE)
