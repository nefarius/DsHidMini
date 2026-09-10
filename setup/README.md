# DsHidMini setup

WixSharp MSI that installs the dual-architecture DsHidMini driver, `igfilter`, ControlApp, nefcon, and the vicius-based updater. ControlApp is a framework-dependent win-x64 app and requires the **.NET 10 Desktop Runtime (x64)**; setup aborts with Error 9001 when that runtime is missing.

Production releases are **not** built from this folder in Visual Studio. Follow [docs/RELEASE.md](../docs/RELEASE.md): tag `vMAJOR.MINOR.PATCH`, submit the partner CAB to Microsoft, ingest the signed package, then run `.\build.cmd BuildSetup`.

## Components

### `nefarius_DsHidMini_Updater.exe`

Software auto-updater. Custom build of [vicius](https://github.com/nefarius/vicius).

### `nefcon\...`

[Driver installation helper](https://github.com/nefarius/nefcon) used to install `igfilter`.

## Staged inputs

`BuildSetup` / `InstallScript` require this layout (created by the release targets, gitignored):

```text
artifacts/drivers/{dshidmini.inf,dshidmini.cat,x64/dshidmini.dll,ARM64/dshidmini.dll}
artifacts/igfilter/nssmkig_{x64,ARM64}/{igfilter.inf,nssmkig.sys}
artifacts/bin/ControlApp.exe
```

`igfilter` is a maintainer-supplied external payload. It is not produced by this repository.

`BuildSetup` verifies the generated MSI contains `ControlApp.exe`, the `DsHidMini Control App` Start Menu shortcut, and the .NET 10 Desktop prerequisite custom action before it EV-signs the package.

Building `DsHidMini.Installer.csproj` without `GenerateMsi=true` only compiles the generator. MSI emission is gated on `.\build.cmd BuildSetup`.

## 3rd party credits

- [WixSharp](https://github.com/oleg-shilo/wixsharp)
- [Nefarius.Utilities.DeviceManagement](https://github.com/nefarius/Nefarius.Utilities.DeviceManagement)
- [CliWrap](https://github.com/Tyrrrz/CliWrap)
- [Nefarius' nŏvīcĭus universal software updater agent for Windows](https://github.com/nefarius/vicius)
- [Json.NET](https://www.newtonsoft.com/json)
