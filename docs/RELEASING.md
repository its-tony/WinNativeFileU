# Preparing a review candidate

Current candidate: 0.1.0-beta.1, Windows 11 x64, unsigned and unpublished. The [M5 report](reports/m5-local-candidate.md) records the exact tested artifact and limits. Preparing a candidate does not authorize a tag, push, GitHub release, signing purchase, certificate trust change or startup activation.

## Build and stage

1. Review `ESTADO.md`, integrity instructions, tasks and failed experiments. Preserve earlier evidence/stagings. Update `build/Version.props` for a new version; review a runtime/dependency change and its notices before updating pins.
2. Run `pwsh -NoProfile -File scripts/build.ps1 -LockedRestore` in an interactive Windows shell. Copy the three TRX files into unique private evidence names before another build overwrites them.
3. Run `pwsh -NoProfile -File scripts/build.ps1 -LockedRestore -Headless` to exercise the CI branch. This excludes only the tray test marked InteractiveShell. It does not replace the full suite or demonstrate remote Actions execution.
4. Prepare the verified portable compiler with `scripts/install-local-inno.ps1`. Review exact product/packager paths and commit them locally before packaging. Keep source MIT and upstream notices unchanged.
5. Run `pwsh -NoProfile -File scripts/package.ps1 -OutputRoot artifacts/<new-unique-candidate-directory>`. The output must not exist. Normal packaging rejects dirty product/packager inputs; `-AllowDirtySource` is diagnostic staging only. The script stages a self-contained folder, notices, attribution, hash manifest, ZIP, unsigned normal setup and separate test-identity setup. It does not install, sign or publish.
6. Inspect `build-info.json`, `files.json`, `BINARY-ATTRIBUTION.json` and `THIRD-PARTY-NOTICES.json`. Check source commit/dirty flags and original license hashes. Build metadata/ZIP timestamps can vary; no bit-for-bit reproducibility claim is made.

## Local qualification

Use `scripts/test-package-integrity.ps1 -PackageFile <package.json>` for extraction/hash checks and intentional changed/unlisted-file rejection on a disposable copy. It preserves the source payload and archive. SHA256SUMS is an integrity record, not publisher authentication.

`scripts/test-installer.ps1 -Stage Prepare -PackageFile <package.json>` uses a separate test identity and private space/Unicode destination. It refuses an existing test installation, checks nonempty-directory refusal, installed manifest, loaded local CLR/WinUI paths, running-app install/uninstall refusal and unchanged startup value. It leaves an isolated resident open. Exercise the installed app and exit normally. Run `-Stage AfterExit -RunFile <returned run.json>` to uninstall only that validated private installation. The harness checks the target, uninstaller hash, absent app process and absence of reparse ancestors. Known files/key must disappear; unknown added file, profile fixture and startup value must remain. Preserve any failure for inspection; never force a cleanup or close.

For visible installed codec checks, `scripts/start-conversion-workbench.ps1 -Executable <installed/WinNativeFileU.App.exe> -ShowWindow` generates disposable images and opens the diagnostic UI. Check actual PNG/BMP conversions, original hashes, independent pixels and normal close; this does not repeat a real picker/radial gesture.

`scripts/test-installer-normal.ps1 -PackageFile <package.json>` additionally exercises the normal identity and actual Start menu shortcut in a fresh private destination, without launching the app. It stops if either a normal registration or shortcut already exists. It verifies manifest and shortcut target/working directory, invokes the scoped native uninstaller and verifies removal/preservation. Default-location installation and the interactive wizard are separate unrun checks.

Record exact hashes, exit codes, immutable journal snapshots and TRX evidence. Review the candidate paths with `python scripts/review-repository.py`, inspect content manually, and commit only public source/docs and reviewed summaries. Keep artifacts, profiles, paths and raw logs private.

## Before public distribution

The manual `Prepare unsigned Windows candidate` workflow only builds and uploads review artifacts; it creates no tag/release. It has not run remotely. Publication requires an explicit owner instruction and a concrete review of the final artifacts and remaining limitations. Reassess notices, dated advisories, signing/publisher identity and clean-machine testing then. Do not bypass Windows security blocks or change trust to make a local test pass.

Current unrun qualification includes clean-machine/default-location/wizard, logon and matching startup-value removal, busy Exit/end-session, picker cancellation, reader/high contrast and additional physical displays. Dependency minimization, journal retention and wider image inputs remain future work. These limitations must accompany any early preview; a candidate is not evidence that they passed.
