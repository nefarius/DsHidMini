# Production setup

CI is the only supported way to produce a signed setup. Dispatch **Build setup**
and choose the driver tag whose attested artifacts should be embedded. Do not
build or sign the MSI locally.

WixSharp MSI that installs the dual-architecture DsHidMini driver, `igfilter`,
ControlApp, nefcon, and the vicius-based updater. ControlApp is a
framework-dependent win-x64 app and requires the **.NET 10 Desktop Runtime
(x64)**; setup aborts with Error 9001 when that runtime is missing.

## Versioning

- Driver tags stay `vMAJOR.MINOR.PATCH` and produce MSI product version
  `MAJOR.MINOR.PATCH`.
- The first setup tag for that payload is `setup-vMAJOR.MINOR.PATCH`. Later
  setup-only re-spins (installer fixes, without rebuilding drivers) use
  `setup-vMAJOR.MINOR.PATCH-r1`, `-r2`, and so on.
- The MSI product version stays `MAJOR.MINOR.PATCH` across re-spins.
- The workflow creates the Git tag on the **setup source** commit (the branch
  or SHA you dispatched from). That can differ from the driver build commit.
- After the setup tag exists, CI opens a **draft** GitHub Release on that tag,
  attaches the signed MSI, and fills in the notes. Review the draft, smoke-test
  the MSI, then publish it. CI never alters an already-published release.

## How to run

1. Finish a tagged `Build` and Partner Center signing so `control-app`,
   `release-metadata`, and `dshidmini-microsoft-drivers` exist.
2. Actions → **Build setup** → Run workflow. Set `driver-tag` to
   `vMAJOR.MINOR.PATCH` (example: `v3.6.0`).
3. The workflow embeds that tag's signed drivers and ControlApp, plus the
   in-repo LFS payload (`nefcon`, updater, `igfilter`), then builds the
   setup from the dispatched ref.
4. On success it uploads artifact `dshidmini-setup`, creates the reserved
   `setup-v*` tag, opens a draft GitHub Release with the signed MSI, then
   mirrors the artifact. A tag collision fails the run without mirroring.
5. Review the draft notes, smoke-test the MSI, then publish the GitHub Release.
   If the draft job failed, create the release by hand from the artifact:

   ```powershell
   gh release create setup-v3.6.0 `
     --title "DsHidMini Driver v3.6.0" `
     --notes-file path\to\notes.md `
     .\Nefarius_DsHidMini_Drivers_x64_arm64_v3.6.0.msi
   ```

## Pending reboot after upgrade

An in-place upgrade can leave the previous driver loaded. After installing, the
`InstallDrivers` action inspects the DsHidMini devnodes (reboot-required flag,
problem code 14, `DN_NEED_RESTART`, bound driver version vs. packaged version)
and, when a reboot is needed, writes these values under
`HKLM\Software\Nefarius Software Solutions e.U.\Nefarius DsHidMini Driver`:

- `RebootPending` (DWORD 1)
- `RebootPendingSince` (UTC ISO 8601 string)
- `RebootPendingReason` (string)

The Exit dialog shows a "Restart now" warning while the marker is set, and
ControlApp shows a Devices page warning until the system has rebooted.

## Outputs

- Actions artifact `dshidmini-setup` (signed MSI plus `setup-metadata.json`)
- Build-mirror copy of the same artifact
- Git tag `setup-vMAJOR.MINOR.PATCH` or `setup-vMAJOR.MINOR.PATCH-rN`
- Draft GitHub Release on that tag (signed MSI plus generated notes)

`setup-metadata.json` records the driver tag, setup tag, driver and setup
commits, source run IDs, and the MSI SHA-256.

## Repository configuration

Tagged driver builds and setup signing need:

- `SIGN_RELAY_SERVER` (variable)
- `SIGN_RELAY_CI_TOKEN` (secret)
- `WEBHOOK_URL` (secret; buildbot artifact mirror)
- `SDCM_PROFILES__DEFAULT__TENANTID` (secret; Partner Center)
- `SDCM_PROFILES__DEFAULT__CLIENTID` (secret; Partner Center)
- `SDCM_PROFILES__DEFAULT__KEY` (secret; Partner Center)

Draft release notes also require GitHub Copilot CLI billed to the organization
so the setup workflow can use `GITHUB_TOKEN` with `copilot-requests: write`.

The setup workflow grants `contents: write` to the tag job and the draft-release
job. The draft-release job also needs `copilot-requests: write` so Highlights
can be generated with the GitHub Copilot CLI (`GITHUB_TOKEN`). The organization
must allow Copilot CLI billed to the organization; otherwise that job fails and
the MSI/tag still remain for a manual release.

Rerunning the draft-release job updates an existing draft and replaces its MSI.
It refuses to change a release that has already been published.

Do not re-sign Microsoft-attested driver binaries. Attestation adds the
Microsoft signature; appending another publisher signature is incorrect.

Building `DsHidMini.Installer.csproj` without `GenerateMsi=true` only compiles
the generator. MSI emission is gated on the GitHub Actions setup workflow.

## Components

### `nefarius_DsHidMini_Updater.exe`

Software auto-updater. Custom build of [vicius](https://github.com/nefarius/vicius).

### `nefcon\...`

[Driver installation helper](https://github.com/nefarius/nefcon) used to install `igfilter`.

### `igfilter\...`

Maintainer-supplied filter payload (`nssmkig_x64` and `nssmkig_ARM64`). Stored
in Git LFS next to nefcon.

## 3rd party credits

- [WixSharp](https://github.com/oleg-shilo/wixsharp)
- [Nefarius.Utilities.DeviceManagement](https://github.com/nefarius/Nefarius.Utilities.DeviceManagement)
- [CliWrap](https://github.com/Tyrrrz/CliWrap)
- [Nefarius' nŏvīcĭus universal software updater agent for Windows](https://github.com/nefarius/vicius)
- [Json.NET](https://www.newtonsoft.com/json)
