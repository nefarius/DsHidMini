# DsHidMini setup

This project generates an MSI package containing x64 and ARM64 driver editions using [WixSharp](https://github.com/oleg-shilo/wixsharp).

## Create a production release

Commands/scripts are to be run from solution root directory.

- Tag a release and let it build on CI
- Use  
  ```PowerShell
  .\stage0.ps1 -Token "xxx" -BuildVersion "3.0.0.0"  
  ```  
  to download the tagged release
- Submit the `*.cab` files to MS Partner Portal for signing
- Place the signed files in `.\artifacts\drivers` directory
- Run  
  ```PowerShell
  .\stage1.ps1
  ```  
  to add EV signatures to binaries
- Run  
  ```PowerShell
  .\stage2.ps1 -SetupVersion 3.0.0
  ```   
  to build and sign an MSI with the given version
- Make public GitHub release
- ???
- Profit!