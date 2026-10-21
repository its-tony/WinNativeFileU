# WinNativeFileU

An original open source Windows 11 file utility, inspired by keyboard-assisted contextual workflows. Files will be processed locally. This project is independent of Tangerine and its developer.

Current scope: **M5 integrated conversion prerequisite verified within documented limits; packaging and public beta pending**. The resident connects Windows image/folder pickers and an Explorer radial to PNG ↔ BMP conversion. Real selected-file and radial batches produced six generated color-grid outputs with exact independently checked pixels. Actual Cancel retained one completed file and left no partial files; normal Exit released resources. See [flow acceptance](docs/reports/m5-conversion-flow.md) and [current status](ESTADO.md). Earlier scope and limits remain in the [M4](docs/reports/m4-resident.md), [M3](docs/reports/m3-completion.md), [M2](docs/reports/m2-completion.md) and [M1](docs/reports/m1-completion.md) reports.

## Development

The latest locked build passed **131 tests**: 47 Core, 54 Windows integration and 30 converters, with no compiler warnings or errors. The tray integration test requires an interactive Windows shell; remote CI has not been run.

In the resident, choose an existing local output folder, then use Choose images and Convert selected. Alternatively, start dragging images in Explorer, hold the mouse button, tap and release **Ctrl+Alt+D**, then drop on an enabled PNG/BMP sector. **Ctrl+Alt+X** rejects the drag; Escape cancels at its source. Choose the output folder again each session. One job runs at a time, with up to eight distinct images of the same source format. Mixed formats and same-format copies are rejected. Each result has a unique suffix; existing files are preserved. Cancel conversion requests cancellation of the running job and retains completed results; that button was verified with a real batch. Exit requests cancellation and waits for cleanup; closing to tray keeps the job running. Close during active work remains unverified.

To launch an isolated flow with generated fixtures and startup controls disabled:

```powershell
pwsh -NoProfile -File scripts/start-image-flow.ps1 -IncludeCancellationFixtures
```

The optional cancellation fixtures use about384MB of disk. The launcher prints a private session.json path; check originals and partial files with `scripts/check-image-flow.ps1 -SessionFile <session.json>`. That checker verifies hashes, not pixels. See [ADR0011](docs/decisions/0011-selected-image-conversion.md). Header eligibility reads at most32bytes per image and is not complete validation; the worker validates the full container and pixels before publication. No callback latency benchmark has been performed.

Running the App without diagnostic arguments opens resident settings. Close hides the window when the preference is enabled; **Ctrl+Alt+W** or the notification icon opens it again. Choose **Exit WinNativeFileU** to stop the resident. Preferences also allow Ctrl+Alt+Shift+W or no shortcut. A shortcut conflict preserves the old binding. Settings live in LocalAppData/WinNativeFileU, with previous versions preserved on save. Invalid/future settings are preserved and require repair before saving. A duplicate resident for the same settings root/session exits; it does not forward activation.

Look for the blue icon with a white diamond, including under hidden notification icons. Its right-click menu offers Open settings, Hide settings and Exit. Check tray icon queries Windows for its location; the diagnostic alone does not prove physical visibility. The earlier generic icon was not found in the owner trial, and that failure remains documented.

The normal settings window offers explicit Enable/Disable start at sign-in. It modifies only the app's matching HKCU Run value; a different command is preserved. Moving the executable or Windows policy may affect sign-in behavior. No real sign-in registration or logon was tested. The isolated test launcher disables these controls:

```powershell
pwsh -NoProfile -File scripts/start-resident.ps1
```

It prints a private launch JSON path. Reuse that profile with `-RunFile <launch.json>` and optionally `-StartHidden`. Check with `scripts/check-resident.ps1 -RunFile <launch.json>`, adding `-ExpectExited` after exit or `-MeasureIdle` for a three-second process sample. Reading the app's Run fingerprint requires an execution context allowed to read HKCU. The harness never writes it. Native lifetime is event-driven, with no polling timer or keyboard hook. See [ADR0010](docs/decisions/0010-resident-lifecycle.md).

M3's engine accepts noninterlaced 8-bit RGB/RGBA PNG and 24/32-bit Windows BMP, subject to decoder validation. Limits: 64 MiB input, 16 million pixels, 16,384 pixels per side, fixed local drives and detected ordinary paths. BMP output requires opaque pixels. Outputs are normalized to sRGB / 96 DPI without copying metadata or profiles. JPEG, WebP, AVIF and PDF conversion are not implemented.

Launch the generated-image conversion workbench after building:

```powershell
pwsh -NoProfile -File scripts/start-conversion-workbench.ps1
```

Select an input, inspect the detected format/dimensions, choose a new output filename and press Convert. Use result as input enables a round trip. The launcher prints a private run.json path; validate original hashes and absence of partial files with `scripts/check-conversion-workbench.ps1 -RunFile <run.json>`. That checker does not validate pixels. Native tests and an independent BMP parser verified the generated color-grid round trip. Cancel is implemented and tested in the engine at four stages; clicking Cancel or closing during active work has not been verified in the UI. The workbench has no file picker or radial handoff. See [ADR0009](docs/decisions/0009-m3-initial-image-engine.md).

Publication uses an exclusive temporary file followed by a move without replacement. Existing destinations, including a late collision, are preserved. Cancellation before publication removes the owned temporary when possible; a cleanup failure reports its path. This is not a hostile-filesystem sandbox or a power-loss durability guarantee.

The separate radial visual preview was validated with53tests (27Core,26integration); M2's integrated source passed **60 tests** (27Core,33integration). Light/Dark appearance, selection, keyboard controls and actual accessible status text have been inspected on Windows. Format labels remain visual examples. See the [preview report](docs/reports/m2-visual.md).

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
dotnet test tests/Converters.Tests/WinNativeFileU.Converters.Tests.csproj --no-build --no-restore -c Release
```

To open the separate M1/M2 experiment with disposable files, a baseline SHA256 manifest and a private journal:

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

No input hooks or automatic startup changes are made. Only an explicit action in normal resident settings can change this app's sign-in entry; isolated test profiles cannot. The unpackaged build includes Windows App SDK components; it still requires .NET 10 x64. Packaging and distribution are future milestones.

## Structure

- src/App: WinUI presentation and lifecycle.
- src/Core: platform-independent domain model; no file I/O.
- src/Converters: bounded Windows PNG/BMP codecs, local path/container validation and new-output publication.
- src/WindowsIntegration: HWND/OLE target with fixture mode or explicit image queue adapter, plus resident tray/hotkeys/settings/ownership/startup adapters.
- tests/Core.Tests: safety boundaries for activation, accepted data and reset.
- tests/Converters.Tests: real codec round trips, pixels, hashes, cancellation, collision and error cases.
- docs/research/tangerine: evidence, functional analysis and Windows feasibility.
- docs/decisions: architecture decisions.
- evidence/private: ignored original research/build output; reviewed summaries belong in docs.

The historical seven-label radial preview remains a separate diagnostic mode. The resident uses a two-format PNG/BMP radial connected to the engine; labels in the historical preview do not imply additional codecs.

## Research and roadmap

Start with [research overview](docs/research/tangerine/overview.md), [sources](docs/research/tangerine/sources.md), [independent specification](docs/functional-specification.md) and [Windows feasibility](docs/research/tangerine/feasibility.md).

R0 → M0 foundation → M1 actual Windows drag feasibility → M2 radial UI → M3 conversion → M4 background integration → M5 beta. **Do not begin the next milestone without the owner's instruction.** Unit tests and a successful build cannot prove an Explorer gesture.
The owner authorized M1 then M2 on 2026-10-19, and M3, M4 and M5 on 2026-10-20. M1–M4 bounded scopes are verified in their reports. M5 connects the conversion workflow first; installer, distribution review and beta preparation remain pending. No push or publication is authorized.

[Contributing](CONTRIBUTING.md) · [Security](SECURITY.md) · [Code of conduct](CODE_OF_CONDUCT.md) · [MIT license](LICENSE)
