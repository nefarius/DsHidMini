# DsHidMini setup

This project generates an MSI package containing x64 and ARM64 driver editions using [WixSharp](https://github.com/oleg-shilo/wixsharp).

## Create a production release

Commands/scripts are to be run from solution root directory.

- Tag a release and let it build on CI (the "Build" GitHub Actions workflow)
- Authenticate the GitHub CLI once with `gh auth login` (needs `repo` scope)
- Use  
  ```PowerShell
  nuke download-ci-artifacts -buildversion "<workflow-run-id>"
  ```  
  to download the tagged release (the run ID is the numeric ID in the workflow run URL)
- Submit the `*.cab` files to MS Partner Portal for signing
- Place the signed files in `.\artifacts\drivers` directory
- Run  
  ```PowerShell
  nuke sign-production-binaries
  ```  
  to add EV signatures to binaries
- Run  
  ```PowerShell
  nuke build-setup -setupversion "3.6.0"
  ```   
  to build and sign an MSI with the given version
- Make public GitHub release
  - Create tag for setup `setup-v3.6.0`
- ???
- Profit!

## Components

### `nefarius_DsHidMini_Updater.exe`

Software auto-updater. Custom build of [vicius](https://github.com/nefarius/vicius).

### `nefcon\...`

[Driver installation helper utility](https://github.com/nefarius/nefcon).

## 3rd party credits

- [WixSharp](https://github.com/oleg-shilo/wixsharp)
- [Nefarius.Utilities.DeviceManagement](https://github.com/nefarius/Nefarius.Utilities.DeviceManagement)
- [CliWrap](https://github.com/Tyrrrz/CliWrap)
- [Nefarius' nŏvīcĭus universal software updater agent for Windows](https://github.com/nefarius/vicius)
- [Json.NET](https://www.newtonsoft.com/json)
