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
    /// the EV-signed partner submission CAB, release-metadata.json, and the Microsoft-attested
    /// driver package when Partner Center signing has finished. Does not sign anything.
    /// </summary>
    [UsedImplicitly]
    public Target DownloadCiArtifacts => _ => _
        .Executes(() =>
        {
            if (string.IsNullOrWhiteSpace(RunId))
            {
                throw new InvalidOperationException(
                    "DownloadCiArtifacts requires RunId (the numeric GitHub Actions run ID from the Build or Partner signing workflow URL).");
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

            DownloadRunArtifact(parsedRunId, downloadDir, "release-metadata", required: true);
            string metadataFile = ReleaseStaging.FindExistingFile(downloadDir, ReleaseStaging.MetadataFileName)
                                  ?? throw new InvalidOperationException(
                                      "Downloaded artifacts are missing release-metadata.json. Use a tagged Build workflow run.");
            ReleaseMetadata sourceMetadata = ReleaseStaging.ReadMetadata(metadataFile);
            long sourceRunId = sourceMetadata.RunId;
            if (sourceRunId != parsedRunId)
            {
                Log.Information(
                    "Requested run {RunId} is a Partner signing retry; ControlApp and the partner CAB come from source run {SourceRunId}",
                    parsedRunId, sourceRunId);
            }

            long payloadRunId = sourceRunId > 0 ? sourceRunId : parsedRunId;
            DownloadRunArtifact(payloadRunId, downloadDir, "control-app", required: true);
            DownloadRunArtifact(payloadRunId, downloadDir, "dshidmini-partner-submission", required: true);
            DownloadRunArtifact(parsedRunId, downloadDir, "dshidmini-microsoft-drivers", required: false);
            if (payloadRunId != parsedRunId)
            {
                DownloadRunArtifact(payloadRunId, downloadDir, "dshidmini-microsoft-drivers", required: false);
            }

            ReleaseStaging.ArrangeDownloadedArtifacts(downloadDir, artifactsDir);
            ReleaseMetadata metadata = ReleaseStaging.ReadMetadata(ReleaseStaging.MetadataPath(artifactsDir));
            if (metadata.RunId != parsedRunId && metadata.RunId != sourceRunId)
            {
                throw new InvalidOperationException(
                    $"Downloaded metadata runId {metadata.RunId} does not match requested RunId {parsedRunId} or source run {sourceRunId}.");
            }

            bool stagedDrivers = ReleaseStaging.TryStageMicrosoftDrivers(downloadDir, artifactsDir);
            Log.Information("Staged release {Tag} / driver {DriverVersion} from run {RunId}",
                metadata.Tag, metadata.DriverVersion, metadata.RunId);
            Log.Information("Partner CAB is in {Submission}", ReleaseStaging.SubmissionDirectory(artifactsDir));
            if (stagedDrivers)
            {
                Log.Information("Microsoft-attested drivers are in {Drivers}",
                    ReleaseStaging.DriversDirectory(artifactsDir));
                Log.Information("Next: StageIgfilter, then ValidateSetupInputs / BuildSetup.");
            }
            else
            {
                Log.Information(
                    "Microsoft-attested drivers are not on this run yet. Wait for Partner Center signing or ingest a downloaded Signed_*.zip.");
            }
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

            ReleaseStaging.ValidateGeneratedMsi(msiPath);
            Log.Information(
                "Generated MSI includes ControlApp.exe, the Start Menu shortcut, and the .NET 10 Desktop prerequisite");

            InvokeSignTool(
                $"sign /v /n \"{SignCertName}\" /tr {SignTimestampUrl} /fd sha256 /td sha256 \"{msiPath}\"");
            ReleaseStaging.RequirePublisherSigner(
                ReleaseStaging.ParseIssuedTo(CaptureSignTool($"verify /pa /all /v \"{msiPath}\"")),
                msiPath);

            string hash = ReleaseStaging.Sha256File(msiPath);
            Log.Information("Signed MSI {Msi} SHA256={Hash}", msiPath, hash);
        });

    /// <summary>
    /// Runs non-production release-pipeline checks: version parsing, staging fixtures, and the
    /// offline Partner Center signing dry run against a mock sdcm.
    /// </summary>
    [UsedImplicitly]
    public Target TestReleasePipeline => _ => _
        .Executes(() =>
        {
            string shell = ToolPathResolver.TryGetEnvironmentExecutable("pwsh.exe")
                           ?? ToolPathResolver.TryGetEnvironmentExecutable("pwsh")
                           ?? TryGetPathExecutable("pwsh")
                           ?? "powershell";
            foreach (string testFile in new[]
                     {
                         "ReleaseVersion.Tests.ps1", "PartnerSigning.Tests.ps1",
                         "PartnerSigning.DryRun.ps1"
                     })
            {
                AbsolutePath tests = RootDirectory / "build" / testFile;
                ProcessTasks.StartProcess(shell, $"-NoProfile -File \"{tests}\"")
                    .AssertZeroExitCode();
            }

            ReleasePipelineTests.Run();
            Log.Information("Release pipeline tests passed");
        });

    static void DownloadRunArtifact(long runId, string downloadDir, string pattern, bool required)
    {
            var process = ProcessTasks.StartProcess("gh",
                $"run download {runId} --repo nefarius/DsHidMini --dir \"{downloadDir}\" --pattern \"{pattern}\"");
            process.WaitForExit();
            if (process.ExitCode == 0)
            {
                return;
            }

            if (required)
            {
                throw new InvalidOperationException($"Failed to download '{pattern}' from GitHub Actions run {runId}.");
            }

            Log.Information("Optional artifact {Pattern} is not on run {RunId}", pattern, runId);
        }

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
