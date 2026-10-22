# Distribution review — local M5 candidate

Date: 2026-10-21, America/Guatemala. **Technical review of the unsigned local 0.1.0-beta.1 candidate; no public release approved.** Source/packager inputs: `1e4ec281f6e1090481d39c8be793a4c29a42a6d9`, with `sourceChangesPresent=false`. Separate tracking/harness changes at packaging time are recorded as `repositoryChangesPresent=true`.

The source retains its existing MIT license. Bundled third-party files retain their own terms. The final payload includes `LICENSE.txt`, `DISTRIBUTION-TERMS.txt`, `THIRD-PARTY-NOTICES.json`, `BINARY-ATTRIBUTION.json` and unchanged notices under `licenses/`. The installer presents source and Windows App SDK binary terms, with the other applicable terms included in the payload. This technical inventory does not constitute legal advice or blanket redistribution approval.

| Payload group | Evidence and retained terms |
|---|---|
| .NET runtime 10.0.13, win-x64 | Self-contained configuration, runtime-pack bytes, original LICENSE and notices. Installed process loaded its bundled coreclr.dll. |
| Windows App SDK 2.5.1 and resolved modules | Original binary license/notices; exact bytes attributed against NuGet and embedded x64 MSIX entries. Installed process loaded its bundled Microsoft.UI.Xaml.dll. |
| Windows SDK.NET.Ref 10.0.22000.57 | Implicit published runtime pack, absent from the App NuGet lock inventory alone. Original official SDK license RTF and CsWinRT license at the commit embedded in WinRT.Runtime 2.2.0.48161 retained with fixed hashes. |
| WebView2 SDK 1.0.3719.77, Windows AI MachineLearning 2.1.74, Tensors 9.0.0 | Included notices; shipped bytes attributed separately. Dependency presence does not demonstrate a browser/AI feature or a bundled full browser runtime. |
| Inno Setup 7.1.0 | Original installer technology notice/license included. Official portable compiler verified by fixed/upstream SHA256 and valid Pyrsys B.V. Authenticode before use. Build tools excluded from product. |

The notice inventory has **16 package/version pairs plus Inno Setup**. The manifest contains **553 files**, with `files.json` itself additional. Binary/resource attribution covers **510 files: 504 exact SHA256 matches and six original compiled/generated outputs**, with zero unknown entries. The modified .NET apphost is explicitly covered by runtime terms. Dependency minimization and size optimization remain future work. SDK/build tools, test packages, private evidence, debug symbols and signing secrets are excluded.

Pinned supplemental originals: [Microsoft SDK license](https://download.microsoft.com/download/0/F/F/0FF2B061-47DD-4F55-89B6-FD1D8C44F14D/sdk_license.rtf), SHA256 `dd07eb178e00c6bba4148457fc00ff77cd4887eb521d504186fe59c9ec8bbe62`; [CsWinRT exact-commit license](https://raw.githubusercontent.com/microsoft/CsWinRT/8649ee3eeb2445ca2a36d80d878ef60b96a6c65d/LICENSE), SHA256 `9906940f61b1f0b533fa7d99baf55178b2808fbe113ea51dfbfad8572ccd5f2b`.

Public NuGet vulnerability query rerun on 2026-10-21: seven projects, exit 0, no vulnerability entries and no stderr. No restore/update occurred. This is a dated feed response for its reported graph; implicit runtime packs, OS codecs and unlisted advisories are not comprehensively audited by it. Evidence: `m5-dependency-audit-02.json`.

Actual local qualification: ZIP integrity and changed/unlisted-file rejection; both installer identities installed/uninstalled in private Unicode/space paths; normal Start menu shortcut target verified then removed; running-app/occupied-directory refusal; installed workbench PNG/BMP round trip with independent exact grid pixels; original files, unknown added files, external profile fixtures and Run value preserved. [Full report](reports/m5-local-candidate.md), [ADR0012](decisions/0012-local-beta-packaging.md).

No certificate was created/trusted or security setting changed. The installer/app are unsigned. Clean-machine deployment, default-location/UI wizard, real logon, matching startup-value removal and remote CI remain unrun. The [release procedure](RELEASING.md) preserves these gates; public distribution needs separate owner authorization.
