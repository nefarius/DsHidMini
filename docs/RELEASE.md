# DsHidMini tagged driver release

This is the maintainer and agent runbook for producing a production MSI. Partner Center upload and download is the only human gate. Every other step is a NUKE target invoked with `.\build.cmd` from the repository root.

Do not use `nuke ...` directly, do not use Visual Studio to emit the MSI, and do not mix artifacts from different GitHub Actions runs.

## Version invariants

| Item | Rule | Example |
|------|------|---------|
| Driver tag | Exactly `vMAJOR.MINOR.PATCH` | `v3.6.0` |
| Setup tag | `setup-vMAJOR.MINOR.PATCH` after the MSI exists | `setup-v3.6.0` |
| Driver file version | `MAJOR.MINOR.PATCH.(2000 + github.run_number)` | `3.6.0.2145` |
| MSI product version | Same three-part value as the driver tag | `3.6.0` |

`2000` is `BUILD_VERSION_OFFSET` in [`.github/workflows/build.yml`](../.github/workflows/build.yml). The revision must stay in `0..65535`.

Rejected tags (the `version` job fails): `v3.6.0.1`, `v3.6.0-pre1`, `setup-v3.6.0`, `sdk-v1.0.0-pre001`.

Non-tag CI (master / pull request) stamps binaries `0.0.0.(2000 + run_number)` and does **not** sign ControlApp, create a partner CAB, or write release metadata.

## Prerequisites

Local machine:

- Windows, `gh` authenticated (`gh auth login`, `repo` scope)
- Visual Studio 2026 / MSBuild 18 and Windows SDK/WDK 10.0.28000 (same pair CI installs)
- EV code-signing certificate whose subject contains `Nefarius Software Solutions e.U.`
- SignTool on PATH via WDK, or pass `--sign-tool-path`
- Maintainer-supplied `igfilter` packages (private; not built or downloaded by this repository)

GitHub Actions secrets/variables used by tagged runs:

- `SIGN_RELAY_SERVER` (variable)
- `SIGN_RELAY_CI_TOKEN` (secret)
- `WEBHOOK_URL` (secret; artifact mirror)

## CI jobs and artifacts

Pushing `vMAJOR.MINOR.PATCH` to `nefarius/DsHidMini` runs [`.github/workflows/build.yml`](../.github/workflows/build.yml):

```text
version
  -> build (x64, ARM64, x86)          unsigned driver DLLs + per-arch CABs
  -> control-app                      EV-signs ControlApp.exe and XInput1_3.dll
  -> partner-cab                      EV-signs driver DLLs, packs dual-arch CAB, EV-signs CAB
```

`partner-cab` does not depend on `control-app`. The compile job must leave `dshidmini.dll` unsigned. Immediately before `makecab` of [`DsHidMini_combined.ddf`](../DsHidMini_combined.ddf), both architecture DLLs are EV-signed. Microsoft attestation **adds** its signature; it does not replace the publisher signature.

| Artifact | When | Contents |
|----------|------|----------|
| `dshidmini-{x64,ARM64,x86}` | every build | unsigned driver/XInput binaries, per-arch CABs, PDBs |
| `control-app` | release tags only | EV-signed `bin/ControlApp.exe` and XInput DLLs |
| `dshidmini-partner-submission` | release tags only | `dshidmini_{DriverVersion}.cab` (EV-signed) |
| `release-metadata` | release tags only | `release-metadata.json` (tag, versions, run ID, CAB SHA-256) |

Per-architecture CABs (`dshidmini_x64.cab` / `dshidmini_ARM64.cab`) are CI archives only. Never submit them to Partner Center.

## Signing identities

| File | After CI `partner-cab` | After Microsoft returns the package | After `BuildSetup` |
|------|------------------------|-------------------------------------|--------------------|
| `dshidmini.dll` (x64, ARM64) | publisher EV | publisher EV **plus** Microsoft attestation | unchanged |
| Partner CAB | publisher EV | n/a (not shipped) | n/a |
| `ControlApp.exe` | publisher EV | n/a | packaged as signed |
| MSI | n/a | n/a | publisher EV |

Do not run the retired `SignProductionBinaries` target. Appending another publisher signature after attestation is incorrect.

## Directory contract

After a successful local staging sequence:

```text
artifacts/
  release-metadata.json
  ci/                             raw gh downloads (do not edit)
  submission/dshidmini_*.cab      EV-signed CAB to upload
  bin/ControlApp.exe              EV-signed
  drivers/
    dshidmini.inf                 dual-arch INF from Microsoft
    dshidmini.cat                 Microsoft-issued catalog
    x64/dshidmini.dll
    ARM64/dshidmini.dll
  igfilter/
    nssmkig_x64/igfilter.inf
    nssmkig_x64/nssmkig.sys
    nssmkig_ARM64/igfilter.inf
    nssmkig_ARM64/nssmkig.sys
```

The catalog is bound to the dual-arch INF. Do not split the package back into per-architecture INFs.

## Procedure

### 1. Tag the driver build

```powershell
git checkout master
git pull
git tag v3.6.0
git push origin v3.6.0
```

Wait for the Build workflow to finish. Copy the numeric run ID from the run URL (`https://github.com/nefarius/DsHidMini/actions/runs/<run-id>`).

Restart point: if the workflow failed, delete the tag only if you will recreate the same three-part version; otherwise use the next patch.

### 2. Download CI outputs

```powershell
.\build.cmd DownloadCiArtifacts --run-id 123456789
```

This target does **not** sign files. It requires `release-metadata`, `control-app`, and `dshidmini-partner-submission` from that exact run and checks the CAB SHA-256 against the metadata.

Restart point: rerun the same command; it replaces `artifacts/ci` and restages ControlApp, metadata, and the CAB.

### 3. Submit the CAB (human gate)

Microsoft documentation: [Attestation sign Windows drivers](https://learn.microsoft.com/en-us/windows-hardware/drivers/dashboard/code-signing-attestation).

1. Open the [Partner Center hardware dashboard](https://partner.microsoft.com/dashboard/hardware).
2. Submit new hardware.
3. Product name: `DsHidMini <setupVersion> <yyyy-MM-dd>` (searchable; not the file version).
4. Upload `artifacts/submission/dshidmini_<driverVersion>.cab`.
5. Leave both test-signing options **unchecked**.
6. Requested signatures: **Windows 10/11 attestation for x64 and ARM64**.
7. Submit and wait until the dashboard marks the submission complete.
8. Download the signed driver package (zip).

This project's verified behavior: Microsoft **adds** its signature to the already EV-signed DLLs and replaces the catalog. If a returned DLL has only a Microsoft signer, stop and investigate; do not continue to MSI.

### 4. Ingest the signed package

```powershell
.\build.cmd IngestMicrosoftPackage --microsoft-package-path "D:\inbox\DsHidMini-signed.zip"
```

Accepts a `.zip`, `.cab`, or an already extracted directory. It locates the unique `dshidmini` folder (the folder that contains `dshidmini.inf`, not the parent), copies its contents into `artifacts/drivers`, and requires:

- layout `dshidmini.inf`, `dshidmini.cat`, `x64/dshidmini.dll`, `ARM64/dshidmini.dll`
- file versions equal to `release-metadata.json` `driverVersion`
- both publisher and Microsoft signers on each DLL

Restart point: rerun with the same or a corrected package; `artifacts/drivers` is replaced.

### 5. Stage igfilter

`igfilter` / `nssmkig` is a private maintainer payload. Point at a directory that already contains both architecture packages:

```powershell
.\build.cmd StageIgfilter --igfilter-path "D:\payloads\igfilter"
```

Required children: `nssmkig_x64\` and `nssmkig_ARM64\` (or `nssmkig_arm64`, which is normalized). Each folder must contain `igfilter.inf` and `nssmkig.sys`. Both `.sys` files must carry the publisher EV signature.

### 6. Validate and build the MSI

```powershell
.\build.cmd ValidateSetupInputs
.\build.cmd BuildSetup --setup-version 3.6.0
```

`BuildSetup` depends on `ValidateSetupInputs`. If `--setup-version` is omitted, the value from `release-metadata.json` is used. If it is supplied, it must match the metadata (and therefore the driver tag).

Output:

```text
setup\Nefarius_DsHidMini_Drivers_x64_arm64_v3.6.0.msi
```

The target EV-signs the MSI, verifies the publisher signature, and logs the SHA-256. Building `setup/DsHidMini.Installer.csproj` without `GenerateMsi=true` only compiles; it does not emit an MSI.

### 7. Publish

```powershell
gh release create setup-v3.6.0 `
  --title "DsHidMini Driver v3.6.0" `
  --notes-file path\to\notes.md `
  .\setup\Nefarius_DsHidMini_Drivers_x64_arm64_v3.6.0.msi
```

`setup-v*` does not trigger the Build workflow. That is intentional.

## Do not

- Submit per-architecture CABs or unsigned DLLs to Partner Center.
- Download a master/PR run and expect a partner CAB or `release-metadata.json`.
- Mix a CAB from run A with a Microsoft package from run B.
- Split the returned dual-arch INF/CAT into x64-only and ARM64-only packages.
- EV-sign driver DLLs again after Microsoft returns them.
- Use `workflow_dispatch` (removed) or a four-part `v*` tag to start a release.
- Treat `artifacts/` as source-controlled input; it is gitignored staging.

## Troubleshooting

| Symptom | Likely cause |
|---------|----------------|
| `version` job: tag must be `vMAJOR.MINOR.PATCH` | Four-part or prerelease tag |
| `DownloadCiArtifacts` missing `release-metadata` | Run was not a three-part release tag |
| Partner CAB hash mismatch | Incomplete download or wrong run ID |
| Multiple `dshidmini` packages | Point `MicrosoftPackagePath` at the zip or the single package folder |
| DLL missing Microsoft signer | Downloaded the submission CAB instead of the dashboard's signed package |
| DLL missing publisher signer | Microsoft package is not from this pipeline's EV-signed CAB |
| `StageIgfilter` cannot find `nssmkig_ARM64` | Source tree is incomplete or named differently |
| MSI build missing files | `ValidateSetupInputs` was skipped or staging was cleaned |
| `BuildSetup` SetupVersion mismatch | Typed `3.6.1` against a `v3.6.0` run |

## Related code

| Path | Role |
|------|------|
| [`build/ReleaseVersion.ps1`](../build/ReleaseVersion.ps1) | Tag parse and four-part version |
| [`build/ReleasePipeline.cs`](../build/ReleasePipeline.cs) | Staging, ingest, validation |
| [`build/ReleaseTargets.cs`](../build/ReleaseTargets.cs) | NUKE entry points |
| [`build/New-PartnerSubmissionInf.ps1`](../build/New-PartnerSubmissionInf.ps1) | Dual-arch INF for the submission CAB |
| [`DsHidMini_combined.ddf`](../DsHidMini_combined.ddf) | Partner CAB layout (`dshidmini/` not at CAB root; includes PDBs) |
| [`setup/InstallScript.cs`](../setup/InstallScript.cs) | WixSharp MSI contents |

Local verification without publishing:

```powershell
.\build.cmd TestReleasePipeline
```
