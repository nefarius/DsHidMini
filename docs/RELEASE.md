# DsHidMini tagged driver release

This is the maintainer and agent runbook for producing a production MSI. Tagged CI EV-signs the combined CAB, submits it to Partner Center, waits for attestation, and stages the Microsoft-signed drivers. The remaining local steps are igfilter staging, MSI construction, and the GitHub release. Every local step is a NUKE target invoked with `.\build.cmd` from the repository root.

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
- `SDCM_PROFILES__DEFAULT__TENANTID` (secret; Partner Center Entra tenant)
- `SDCM_PROFILES__DEFAULT__CLIENTID` (secret; Partner Center app)
- `SDCM_PROFILES__DEFAULT__KEY` (secret; Partner Center API key)

## CI jobs and artifacts

Pushing `vMAJOR.MINOR.PATCH` to `nefarius/DsHidMini` runs [`.github/workflows/build.yml`](../.github/workflows/build.yml):

```text
version
  -> build (x64, ARM64, x86)          unsigned driver DLLs + per-arch CABs
  -> control-app                      EV-signs ControlApp.exe and XInput1_3.dll
  -> partner-cab                      EV-signs driver DLLs, packs dual-arch CAB, EV-signs CAB
  -> partner-signing                  Partner Center attestation via [partner-signing.yml](../.github/workflows/partner-signing.yml)
       create -> upload -> wait -> ingest
```

`partner-cab` does not depend on `control-app`. The compile job must leave `dshidmini.dll` unsigned. Immediately before `makecab` of [`DsHidMini_combined.ddf`](../DsHidMini_combined.ddf), both architecture DLLs are EV-signed. Microsoft attestation **adds** its signature; it does not replace the publisher signature.

| Artifact | When | Contents |
|----------|------|----------|
| `dshidmini-{x64,ARM64,x86}` | every build | unsigned driver/XInput binaries, per-arch CABs, PDBs |
| `control-app` | release tags only | EV-signed `bin/ControlApp.exe` and XInput DLLs |
| `dshidmini-partner-submission` | release tags only | `dshidmini_{DriverVersion}.cab` (EV-signed) |
| `release-metadata` | release tags only | `release-metadata.json` (tag, versions, run ID, CAB SHA-256) |
| `partner-signing-checkpoint` | release tags only | Partner product/submission IDs (non-secret) |
| `dshidmini-partner-signed` | release tags only | `Signed_<id>.zip` and `Initial_<id>.cab` from the portal, mirrored to buildbot |
| `partner-signing-result` | release tags only | Portal URL, IDs, and signed file names |
| `dshidmini-microsoft-drivers` | release tags only | Validated dual-arch `dshidmini.inf` / `.cat` / `x64` / `ARM64` tree |

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

Wait for the Build workflow, including Partner Center signing, to finish. Signing can take up to about an hour after the CAB is packed. Copy the numeric run ID from the run URL (`https://github.com/nefarius/DsHidMini/actions/runs/<run-id>`).

Restart point: if the workflow failed, delete the tag only if you will recreate the same three-part version; otherwise use the next patch.

### 2. Download CI outputs

```powershell
.\build.cmd DownloadCiArtifacts --run-id 123456789
```

This target does **not** sign files. It requires `release-metadata`, `control-app`, and `dshidmini-partner-submission`. After Partner Center signing finishes it also pulls `dshidmini-microsoft-drivers` into `artifacts/drivers`. It checks the CAB SHA-256 against the metadata.

If you re-ran [`.github/workflows/partner-signing.yml`](../.github/workflows/partner-signing.yml) by hand, pass that signing run ID instead. ControlApp and the partner CAB are then fetched from the source Build run recorded in `release-metadata.json`.

Restart point: rerun the same command; it replaces `artifacts/ci` and restages ControlApp, metadata, the CAB, and attested drivers when present.

### 3. Partner Center signing (CI)

The `partner-signing` jobs create a new versioned Attestation product named `DsHidMini <setupVersion> <driverVersion>`, or resume one named by the `product-id` / `submission-id` inputs, and request exactly:

- `WINDOWS_v100_X64_RS5_FULL` (Windows 10 Client version 1809 Client x64 (RS5))
- `WINDOWS_v100_ARM64_RS5_FULL` (Windows 10 Client version 1809 Client ARM64 (RS5))

They upload the EV-signed combined CAB, wait up to 60 minutes, download `Signed_<id>.zip` plus `Initial_<id>.cab`, mirror that pair to buildbot, then ingest and signature-check the signed zip.

Retry without rebuilding. Every fresh run opens another Partner Center product, so prefer the resume path:

- **Re-run failed jobs** on the same workflow run resumes after the last successful job (`create` / `upload` / `wait` / `ingest`). Upload/commit is status-aware and will not blindly re-upload a submission that already advanced. A re-run replays the commit the run started from, so a fix to `build/PartnerSigning.ps1` or to the workflow is **not** picked up; dispatch instead.
- **workflow_dispatch** with the original Build run ID plus `product-id` and `submission-id` resumes that existing submission using the current branch's scripts. This is how a script fix reaches a submission Hardware Dev Center is already processing, without opening another product.
- **workflow_dispatch** with the original Build run ID and no `product-id` / `submission-id` creates a fresh product and submission from the already-built CAB. Use this only once Hardware Dev Center has rejected or failed a submission.

Before changing that automation, run `.\build.cmd TestReleasePipeline`. It runs the workflow's own `create` / `upload` / `wait` scripts against a mock `sdcm` 1.0.0-pre004 (`submission get` / `status` / `--overwrite`) and asserts which verbs each stage calls, so submission-state bugs surface locally instead of consuming a product.

This project's verified behavior: Microsoft **adds** its signature to the already EV-signed DLLs and replaces the catalog. If a returned DLL has only a Microsoft signer, stop and investigate; do not continue to MSI.

CI does **not** build or publish the MSI, create a GitHub release, or create a shipping label.

### 4. Ingest the signed package (only if CI did not)

Skip this when `DownloadCiArtifacts` already staged `artifacts/drivers`. Use it for a portal zip downloaded by hand:

```powershell
.\build.cmd IngestMicrosoftPackage --microsoft-package-path "D:\inbox\Signed_1152921505701840714.zip"
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
- Use a four-part `v*` tag to start a release. `workflow_dispatch` on Partner Center signing is only for retrying attestation from an existing Build run.
- Dispatch Partner Center signing to debug the automation. Each fresh run opens a Partner Center product; resume the existing submission or run `.\build.cmd TestReleasePipeline` instead.
- Treat `artifacts/` as source-controlled input; it is gitignored staging.

## Troubleshooting

| Symptom | Likely cause |
|---------|----------------|
| `version` job: tag must be `vMAJOR.MINOR.PATCH` | Four-part or prerelease tag |
| `DownloadCiArtifacts` missing `release-metadata` | Run was not a three-part release tag |
| Partner CAB hash mismatch | Incomplete download or wrong run ID |
| `create` job auth exit 2 | `SDCM_PROFILES__DEFAULT__*` secrets missing or invalid |
| `wait` job exit 7 | Hardware Dev Center rejected the submission; dispatch a new signing run |
| `wait` job exit 9 | `--wait-timeout` 3600 elapsed; re-run failed jobs to resume the wait |
| `wait` job: submission is still commitPending | The commit never landed; re-run the `upload` job before the wait |
| `create` job: product-id and submission-id must be supplied together | Resume needs both ids, or neither |
| Missing `Signed_<id>.zip` / `Initial_<id>.cab` pair | Downloaded the wrapper or submission CAB instead of the portal pair |
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
| [`build/PartnerSigning.ps1`](../build/PartnerSigning.ps1) | SDCM payloads, `submission status` wrappers, Signed_/Initial_ pair checks |
| [`build/PartnerSigning.DryRun.ps1`](../build/PartnerSigning.DryRun.ps1) | Offline dry run of the signing workflow against a mock sdcm |
| [`.github/workflows/partner-signing.yml`](../.github/workflows/partner-signing.yml) | Retryable Partner Center submit / wait / ingest |
| [`build/New-PartnerSubmissionInf.ps1`](../build/New-PartnerSubmissionInf.ps1) | Dual-arch INF for the submission CAB |
| [`DsHidMini_combined.ddf`](../DsHidMini_combined.ddf) | Partner CAB layout (`dshidmini/` not at CAB root; includes PDBs) |
| [`setup/InstallScript.cs`](../setup/InstallScript.cs) | WixSharp MSI contents |

Local verification without publishing:

```powershell
.\build.cmd TestReleasePipeline
```
