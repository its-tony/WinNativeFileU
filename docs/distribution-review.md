# Distribution review — M5 preparation

Date: 2026-10-20, America/Guatemala. **In progress; no package or public beta approved.** This is a technical inventory, not a statement that all redistribution obligations have been satisfied.

The application source retains its existing MIT license. Third-party runtime files have their own terms; the source license does not relicense those binaries. The App lock graph contains16NuGet package/version pairs. Local nuspec/license/notice metadata was inventoried in private evidence m5-dependency-inventory-01.json. The final publish directory and .NET runtime files have not yet been attributed, so this is not a complete distribution bill of materials.

| Group | Observed metadata | Remaining work |
|---|---|---|
| Windows App SDK2.5.1 and modules | Local Microsoft Windows App SDK terms and notices; redistribution section covers files placed with the app by the package, subject to its requirements | Retain applicable terms/notices and review the exact payload before distribution |
| WebView2 SDK1.0.3719.77 | Local license and notice files | Attribute any shipped loader/SDK files; do not infer bundled browser runtime from the dependency graph |
| Windows AI MachineLearning2.1.74 | Local license and ThirdPartyNotices | Distinguish package dependency from app usage and actual shipped files; no AI feature is implemented here |
| System.Numerics.Tensors9.0.0 | MIT expression, license and third-party notices | Include required notices if present in final payload |
| Windows SDK BuildTools/MSIX | Build-time tools, including a license URL or local SDK license | Do not copy build tools into the product payload |
| .NET runtime | Development SDK present; current App build is framework-dependent for .NET | Choose and verify deployment model, inventory any bundled runtime and include its notices |

Installed SDK10.0.402 help confirms `dotnet package list --no-restore --include-transitive --vulnerable --format json --source …`. Querying the public NuGet.org source for the seven-project solution returned exit0, no stderr and no vulnerability entries. This is a dated feed response, not proof of absence of vulnerabilities or a license audit. No restore or package update was performed. Evidence m5-dependency-audit-01.json.

Windows SDK22000/x64 contains MakeAppx and Signtool. MakeAppx usage printed its supported commands; no package was built. An earlier check limited to PATH/NuGet cache did not cover this installed SDK directory. [Microsoft's deployment overview](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/) describes the packaging choices. [Microsoft's signing guide](https://learn.microsoft.com/en-us/windows/msix/package/sign-msix-package-guide) documents signing and test trust requirements. No certificate was created or trusted, and no security setting was changed. Tool availability is not an installed-product test.

Remaining M5 gates: accepted integrated flow, versioning, reproducible payload, complete notices/attribution, installer and isolated install/uninstall verification, regression checks on that payload, CI artifact preparation, user documentation and release review. Remote CI and clean-machine deployment are unrun. Publication still requires the owner's approval.
