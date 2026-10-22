# WinNativeFileU 0.1.0-beta.1

An early Windows11x64 image utility. It converts supported PNG and BMP images locally and writes new files. JPEG, WebP, AVIF and PDF are not implemented. Keep backups of files you care about; this beta is not qualified against a broad image corpus or hostile filesystem changes.

## Install or run

The setup installs for your current user, normally under LocalAppData/Programs/WinNativeFileU. It requests no administrator access, adds a Start menu shortcut, and does not launch the app or enable sign-in startup automatically. Exit any running WinNativeFileU first. This beta requires an empty destination and refuses an existing installed version. To replace it, uninstall first, then install again. Settings and converted files are retained.

The ZIP is an alternative: extract its entire contents into an empty folder and run WinNativeFileU.App.exe. Keep all DLLs, resource folders, manifests and license files together. .NET10 and Windows App SDK runtime files are included; there is no single-file portable executable. No package identity or Microsoft Store registration is used.

Locally prepared beta artifacts are unsigned. There is no verified publisher signature or SmartScreen reputation claim. Do not change security settings or bypass a Windows block to run an artifact. Compare SHA256SUMS with the copy supplied by the publisher; checksums detect changed content but do not establish publisher trust. Public distribution requires a separate release/signing review.

## Convert images

1. Open WinNativeFileU and choose an existing local output folder. Select the folder again each app session.
2. Use Choose images to select one to eight distinct images of the same source format.
3. Select PNG for BMP inputs, or BMP for PNG inputs, then press Convert selected.
4. Read the status and per-file results. Open output folder shows newly created files with unique suffixes.

For Explorer dragging: select supported images, start dragging and hold the mouse button. Tap and release Ctrl+Alt+D, move onto an enabled PNG/BMP sector and release the mouse. The radial only opens while the left button is held, an output folder is selected, and no picker/job is active. Releasing the shortcut does not close it. A successful drop queues conversion; check the main window for its result.

The center and disabled sectors reject delivery. Ctrl+Alt+X rejects/closes the current drag target; Escape cancels the drag at its source. Dropping outside may leave the radial visible until Ctrl+Alt+X. These drag controls do not cancel an already running conversion.

Cancel conversion stops the current job before its next publication when possible. Already completed files remain. A file that finishes at the cancellation boundary may still be retained. The app reports errors per file; CleanupFailed means a temporary could not be removed, and its path is shown. No batch rollback is promised.

## Resident controls

The blue notification icon with a white diamond offers Open settings, Hide settings and Exit. Look under hidden icons if necessary. Ctrl+Alt+W shows the panel by default; Resident settings can change or disable that shortcut. Closing the panel normally keeps the app in the tray. Use Exit WinNativeFileU to terminate it.

Hide to tray leaves an active conversion running. Exit requests cancellation and waits for cleanup; the busy-exit path is implemented but remains unverified. The installer/uninstaller refuses to proceed while any WinNativeFileU mode is running in the same Windows session.

Start at sign-in is disabled unless you explicitly enable it in normal Resident settings. Installer and ZIP extraction do not enable it. Moving the executable after enabling startup may leave a different command which the app preserves. Disable it from the old installation before moving if possible. The uninstaller removes only an exactly matching command for its own executable; a different value remains. Actual logon and that matching-value removal have not been exercised in the beta qualification.

## Supported inputs and limits

- Noninterlaced8-bit RGB/RGBA PNG;24/32-bit Windows BMP, subject to full decoder/container validation.
- BMP output requires opaque pixels; transparency is not silently flattened.
- At most64MiB input,16million pixels and16384pixels per side.
- Fixed local drives and detected ordinary paths. UNC, device/alternate-stream and detected reparse/offline/recall paths are rejected.
- Outputs use sRGB/96DPI. Original metadata/profiles are not copied.
- Original files and existing destinations are preserved. Temporary publication is exclusive but is not a power-loss durability or hostile-filesystem guarantee.

Header eligibility is preliminary; a selectable image can still fail full validation. An unavailable shortcut or radial does not imply conversion by dragging is active; use the pickers and read the status.

## Uninstall and retained data

Exit the app, then use Windows Installed apps or unins000.exe inside its installation folder. The uninstaller removes its installed files and shortcut. It preserves LocalAppData/WinNativeFileU settings, backups and diagnostic journals, your selected output folder and unrecognized files added to the installation folder. Do not store personal files in that folder.

Diagnostic journals contain local file paths and results. Keep them private; share only a reviewed excerpt. The app has no journal-retention/pruning policy yet. Startup, filesystem and registry checks do not protect against malicious concurrent modification.

The current physical checks cover one display at DPI144. Screen-reader, physical high contrast, additional monitors, clean-machine deployment and real Windows logon/Explorer restart remain outside the demonstrated scope. See the repository's milestone reports for the exact evidence and limitations.

## Licenses

Original source is MIT licensed. Bundled runtime components retain their own terms and notices under licenses/. DISTRIBUTION-TERMS.txt includes the source license and Microsoft Windows App SDK binary terms. THIRD-PARTY-NOTICES.json inventories the staged package notices; it is not a blanket relicense of all binaries. Installer technology: Inno Setup by Jordan Russell and Martijn Laan, used without modification.
