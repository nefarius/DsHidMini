using System;
using System.IO;
using System.Linq;

using JetBrains.Annotations;

using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tooling;

using Serilog;

partial class Build
{
    AbsolutePath ReleaseDownloadDirectory => ResolvedArtifactsPath / "ci";

    SetupInputReport _validatedSetupInputs;

    /// <summary>
    /// Downloads the tagged-run artifacts needed to continue a release: signed ControlApp,
    /// the EV-signed partner submission CAB, and release-metadata.json. Does not sign anything.
    /// </summary>
    [UsedImplicitly]
    public Target DownloadCiArtifacts => _ => _
        .Executes(() =>
        {
            if (string.IsNullOrWhiteSpace(RunId))
            {
                throw new InvalidOperationException(
                    "DownloadCiArtifacts requires RunId (the numeric GitHub Actions run ID from the Build workflow URL).");
            }

            if (!long.TryParse(RunId, out long parsedRunId) || parsedRunId <= 0)
            {
                throw new InvalidOperationException($"RunId must be a positive GitHub Actions run ID. Got: '{RunId}'.");
            }

            string artifactsDir = ResolvedArtifactsPath;
            string downloadDir = ReleaseDownloadDirectory;
            if (Directory.Exists(downloadDir))
            {
                Directory.Delete(downloadDir, recursive: true);
            }

            Directory.CreateDirectory(downloadDir);

            string[] patterns = ["release-metadata", "control-app", "dshidmini-partner-submission"];
            foreach (string pattern in patterns)
            {
                ProcessTasks.StartProcess("gh",
                        $"run download {RunId} --repo nefarius/DsHidMini --dir \"{downloadDir}\" --pattern \"{pattern}\"")
                    .AssertZeroExitCode();
            }

            ReleaseStaging.ArrangeDownloadedArtifacts(downloadDir, artifactsDir);
            ReleaseMetadata metadata = ReleaseStaging.ReadMetadata(ReleaseStaging.MetadataPath(artifactsDir));
            if (metadata.RunId != parsedRunId)
            {
                throw new InvalidOperationException(
                    $"Downloaded metadata runId {metadata.RunId} does not match requested RunId {parsedRunId}.");
            }

            Log.Information("Staged release {Tag} / driver {DriverVersion} from run {RunId}",
                metadata.Tag, metadata.DriverVersion, metadata.RunId);
            Log.Information("Partner CAB is in {Submission}", ReleaseStaging.SubmissionDirectory(artifactsDir));
            Log.Information("Next: submit that CAB to Partner Center, then IngestMicrosoftPackage.");
        });

    /// <summary>
    /// Copies the unique dshidmini package returned by Microsoft into artifacts/drivers.
    /// </summary>
    [UsedImplicitly]
    public Target IngestMicrosoftPackage => _ => _
        .Executes(() =>
        {
            string source = MicrosoftPackagePath;
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new InvalidOperationException(
                    "IngestMicrosoftPackage requires MicrosoftPackagePath (zip, cab, or extracted directory).");
            }

            string artifactsDir = ResolvedArtifactsPath;
            ReleaseMetadata metadata = ReleaseStaging.ReadMetadata(ReleaseStaging.MetadataPath(artifactsDir));
            ReleaseStaging.IngestMicrosoftPackage(source, ReleaseStaging.DriversDirectory(artifactsDir));

            string x64Dll = Path.Combine(ReleaseStaging.DriversDirectory(artifactsDir), "x64", "dshidmini.dll");
            string armDll = Path.Combine(ReleaseStaging.DriversDirectory(artifactsDir), "ARM64", "dshidmini.dll");
            Version expected = Version.Parse(metadata.DriverVersion);
            ReleaseStaging.RequireFileVersion(x64Dll, expected);
            ReleaseStaging.RequireFileVersion(armDll, expected);

            Func<string, string> verify = CaptureSignTool;
            ReleaseStaging.RequireDualDriverSigners(
                ReleaseStaging.ParseIssuedTo(verify($"verify /pa /all /v \"{x64Dll}\"")), x64Dll);
            ReleaseStaging.RequireDualDriverSigners(
                ReleaseStaging.ParseIssuedTo(verify($"verify /pa /all /v \"{armDll}\"")), armDll);

            Log.Information("Ingested Microsoft-attested package for {DriverVersion} into {Drivers}",
                metadata.DriverVersion, ReleaseStaging.DriversDirectory(artifactsDir));
        });

    /// <summary>
    /// Copies maintainer-supplied igfilter packages into artifacts/igfilter.
    /// </summary>
    [UsedImplicitly]
    public Target StageIgfilter => _ => _
        .Executes(() =>
        {
            if (string.IsNullOrWhiteSpace(IgfilterPath))
            {
                throw new InvalidOperationException(
                    "StageIgfilter requires IgfilterPath (directory containing nssmkig_x64 and nssmkig_ARM64).");
            }

            ReleaseStaging.StageIgfilter(IgfilterPath, ReleaseStaging.IgfilterDirectory(ResolvedArtifactsPath));
            Log.Information("Staged igfilter packages into {Igfilter}",
                ReleaseStaging.IgfilterDirectory(ResolvedArtifactsPath));
        });

    /// <summary>
    /// Verifies the staged MSI inputs, including publisher-plus-Microsoft driver signatures.
    /// </summary>
    [UsedImplicitly]
    public Target ValidateSetupInputs => _ => _
        .Executes(() =>
        {
            _validatedSetupInputs = ReleaseStaging.ValidateSetupInputs(
                ResolvedArtifactsPath,
                SetupVersion,
                requireSignatures: true,
                verifyTool: CaptureSignTool);

            Log.Information("Setup inputs are valid for {SetupVersion} (driver {DriverVersion}, tag {Tag}, run {RunId})",
                _validatedSetupInputs.ResolvedSetupVersion,
                _validatedSetupInputs.DriverVersion,
                _validatedSetupInputs.Metadata.Tag,
                _validatedSetupInputs.Metadata.RunId);
        });

    /// <summary>
    /// Builds and EV-signs the MSI after validating staged inputs.
    /// </summary>
    [UsedImplicitly]
    public Target BuildSetup => _ => _
        .DependsOn(ValidateSetupInputs)
        .Executes(() =>
        {
            SetupInputReport report = _validatedSetupInputs
                ?? throw new InvalidOperationException("Setup inputs were not validated.");

            string setupVersion = report.ResolvedSetupVersion;
            AbsolutePath setupProject = RootDirectory / "setup" / "DsHidMini.Installer.csproj";
            if (!File.Exists(setupProject))
            {
                throw new InvalidOperationException($"Setup project not found at {setupProject}");
            }

            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(setupProject)
                .SetConfiguration(Configuration.Release)
                .SetProperty("SetupVersion", setupVersion)
                .SetProperty("GenerateMsi", true));

            string msiName = $"Nefarius_DsHidMini_Drivers_x64_arm64_v{setupVersion}.msi";
            AbsolutePath msiInSetup = RootDirectory / "setup" / msiName;
            AbsolutePath msiInBin = RootDirectory / "setup" / "bin" / "Release" / "net48" / msiName;
            AbsolutePath msiPath = File.Exists(msiInSetup) ? msiInSetup : msiInBin;
            if (!File.Exists(msiPath))
            {
                throw new InvalidOperationException($"MSI not found: {msiInSetup} or {msiInBin}");
            }

            InvokeSignTool(
                $"sign /v /n \"{SignCertName}\" /tr {SignTimestampUrl} /fd sha256 /td sha256 \"{msiPath}\"");
            ReleaseStaging.RequirePublisherSigner(
                ReleaseStaging.ParseIssuedTo(CaptureSignTool($"verify /pa /all /v \"{msiPath}\"")),
                msiPath);

            string hash = ReleaseStaging.Sha256File(msiPath);
            Log.Information("Signed MSI {Msi} SHA256={Hash}", msiPath, hash);
        });

    /// <summary>
    /// Runs non-production release-pipeline unit checks (version parsing and staging fixtures).
    /// </summary>
    [UsedImplicitly]
    public Target TestReleasePipeline => _ => _
        .Executes(() =>
        {
            AbsolutePath tests = RootDirectory / "build" / "ReleaseVersion.Tests.ps1";
            string shell = ToolPathResolver.TryGetEnvironmentExecutable("pwsh.exe")
                           ?? ToolPathResolver.TryGetEnvironmentExecutable("pwsh")
                           ?? TryGetPathExecutable("pwsh")
                           ?? "powershell";
            ProcessTasks.StartProcess(shell, $"-NoProfile -File \"{tests}\"")
                .AssertZeroExitCode();

            ReleasePipelineTests.Run();
            Log.Information("Release pipeline tests passed");
        });

    static string TryGetPathExecutable(string name)
    {
        try
        {
            return ToolPathResolver.GetPathExecutable(name);
        }
        catch (Exception)
        {
            return null;
        }
    }

    string CaptureSignTool(string arguments)
    {
        if (!string.IsNullOrWhiteSpace(SignToolPath) && File.Exists(SignToolPath))
        {
            return string.Join(Environment.NewLine,
                ProcessTasks.StartProcess(SignToolPath, arguments)
                    .AssertZeroExitCode()
                    .Output
                    .Select(line => line.Text));
        }

        var result = WdkWhere.Invoke($"run signtool {arguments:nq}");
        return string.Join(Environment.NewLine, result.Select(line => line.Text));
    }
}
