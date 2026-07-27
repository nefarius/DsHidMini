using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using JetBrains.Annotations;

using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tooling;

using Serilog;

class Build : NukeBuild
{
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution]
    readonly Solution Solution;

    [Parameter("Target platform for BuildDmf on CI (x64, ARM64 or x86). Not needed for local builds.")]
    readonly string TargetPlatform = "";

    [Parameter("GitHub Actions run ID for DownloadCiArtifacts artifact download")]
    readonly string BuildVersion = "";

    [Parameter("Output path for DownloadCiArtifacts artifacts. Default: ./artifacts")]
    readonly string ArtifactsPath = "./artifacts";

    [Parameter("Skip signing in DownloadCiArtifacts")]
    readonly bool NoSigning;

    [Parameter("Setup version for BuildSetup (e.g. 3.0.0)")]
    readonly string SetupVersion = "";

    [Parameter("Path to signtool.exe. When not set, Nefarius.Tools.WDKWhere is used to run signtool.")]
    readonly string SignToolPath = "";

    [NuGetPackage("Nefarius.Tools.WDKWhere", "wdkwhere.dll", Framework = "net8.0")]
    readonly Tool WdkWhere;

    const string SignTimestampUrl = "http://timestamp.digicert.com";
    const string SignCertName = "Nefarius Software Solutions e.U.";

    AbsolutePath DmfSolution => Solution.Directory / "DMF/Dmf.sln";

    AbsolutePath ResolvedArtifactsPath => (AbsolutePath)Path.GetFullPath(Path.Combine(RootDirectory, ArtifactsPath));

    /// <summary>
    /// Version stamp propagated from CI (BUILD_VERSION env var, set from github.run_number). Empty for local builds.
    /// </summary>
    static string BuildVersionStamp => Environment.GetEnvironmentVariable("BUILD_VERSION");

    /// <summary>
    /// Runs Microsoft's SignTool with the provided command-line arguments, using the explicit SignToolPath when available or delegating to the WdkWhere tool otherwise.
    /// </summary>
    /// <param name="arguments">Command-line arguments to pass to SignTool (e.g., certificate, timestamp and file options).</param>
    void InvokeSignTool(string arguments)
    {
        if (!string.IsNullOrWhiteSpace(SignToolPath) && File.Exists(SignToolPath))
        {
            ProcessTasks.StartProcess(SignToolPath, arguments).AssertZeroExitCode();
        }
        else
        {
            var cmd = "run signtool " + arguments;
            WdkWhere.Invoke($"{cmd:nq}");
        }
    }

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
        });

    /// <summary>
    /// Restores ControlApp for win-x64 (used only by PublishControlApp so project.assets.json has the RID target).
    /// </summary>
    Target Restore => _ => _
        .Executes(() =>
        {
            AbsolutePath controlAppProjectPath = RootDirectory / "ControlApp" / "ControlApp.csproj";
            if (!File.Exists(controlAppProjectPath))
            {
                throw new InvalidOperationException($"ControlApp project not found at {controlAppProjectPath}");
            }

            DotNetTasks.DotNetRestore(s => s
                .SetProjectFile(controlAppProjectPath)
                .SetRuntime("win-x64"));
        });

    Target BuildDmf => _ => _
        .Executes(() =>
        {
            Log.Information("DMF solution path: {DmfSolution}", DmfSolution);

            IEnumerable<(Configuration config, MSBuildTargetPlatform platform)> buildCombinations;
            if (IsLocalBuild)
            {
                Configuration[] configs = [Configuration.Debug, Configuration.Release];
                MSBuildTargetPlatform[] platforms = [MSBuildTargetPlatform.x64, (MSBuildTargetPlatform)"ARM64"];
                buildCombinations = configs.SelectMany(c => platforms.Select(p => (c, p)));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(TargetPlatform))
                {
                    throw new InvalidOperationException(
                        "TargetPlatform must be set on CI, e.g. --target-platform x64.");
                }

                if (string.Equals(TargetPlatform, "x86", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(TargetPlatform, "Win32", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Warning("DMF dropped 32-Bit support, skipping build");
                    return;
                }

                MSBuildTargetPlatform platform = string.Equals(TargetPlatform, "ARM64", StringComparison.OrdinalIgnoreCase)
                    ? (MSBuildTargetPlatform)"ARM64"
                    : MSBuildTargetPlatform.x64;
                buildCombinations = [(Configuration, platform)];
            }

            foreach ((Configuration config, MSBuildTargetPlatform platform) in buildCombinations)
            {
                Log.Information("Building DMF {Configuration} | {Platform}", config, platform);
                MSBuildTasks.MSBuild(s => s
                    .SetTargetPath(DmfSolution)
                    .SetTargets("Build")
                    .SetConfiguration(config)
                    .SetTargetPlatform(platform)
                    .SetMaxCpuCount(Environment.ProcessorCount)
                    .SetNodeReuse(IsLocalBuild)
                    .SetVerbosity(MSBuildVerbosity.Minimal)
                );
            }
        });

    Target Compile => _ => _
        .DependsOn(BuildDmf)
        .Executes(() =>
        {
            Logging.Level = LogLevel.Normal;

            MSBuildTasks.MSBuild(s =>
            {
                MSBuildSettings settings = s
                    .SetTargetPath(Solution)
                    .SetTargets("Rebuild")
                    .SetConfiguration(Configuration)
                    .SetMaxCpuCount(Environment.ProcessorCount)
                    .SetNodeReuse(IsLocalBuild)
                    .SetVerbosity(MSBuildVerbosity.Minimal);

                // Aggressively silence C# warnings for local Nuke builds (nullability, CS8981, XML docs, etc.)
                if (IsLocalBuild)
                {
                    string noWarn =
                        "CS0219;CS1587;CS1591;CS8600;CS8601;CS8602;CS8603;CS8604;CS8618;CS8619;CS8622;CS8625;CS8629;CS8765;CS8767;CS8981";
                    settings = settings.SetProperty("NoWarn", noWarn.Replace(";", "%3B"));
                }

                // Stamps managed projects (ControlApp, SDK, ipctest, installer) with the CI build version,
                // replacing AppVeyor's dotnet_csproj auto-patching. C++ projects ignore unknown properties.
                if (!string.IsNullOrWhiteSpace(BuildVersionStamp))
                {
                    settings = settings
                        .SetProperty("Version", BuildVersionStamp)
                        .SetProperty("AssemblyVersion", BuildVersionStamp)
                        .SetProperty("FileVersion", BuildVersionStamp)
                        .SetProperty("InformationalVersion", BuildVersionStamp);
                }

                return settings;
            });
        });

    /// <summary>
    /// Publishes the ControlApp as a production-ready, single-file, framework-dependent executable for win-x64.
    /// Output is written to the solution's bin folder (same layout as the former release-win-x64 publish profile).
    /// </summary>
    [UsedImplicitly]
    public Target PublishControlApp => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            AbsolutePath controlAppProjectPath = RootDirectory / "ControlApp" / "ControlApp.csproj";
            if (!File.Exists(controlAppProjectPath))
            {
                throw new InvalidOperationException($"ControlApp project not found at {controlAppProjectPath}");
            }

            AbsolutePath publishOutput = RootDirectory / "bin";

            DotNetTasks.DotNetPublish(s =>
            {
                string noWarn =
                    "CS0219;CS1587;CS1591;CS8600;CS8601;CS8602;CS8603;CS8604;CS8618;CS8619;CS8622;CS8625;CS8629;CS8765;CS8767;CS8981";
                s = s.SetProperty("NoWarn", noWarn.Replace(";", "%3B"));

                if (!string.IsNullOrWhiteSpace(BuildVersionStamp))
                {
                    s = s
                        .SetProperty("Version", BuildVersionStamp)
                        .SetProperty("AssemblyVersion", BuildVersionStamp)
                        .SetProperty("FileVersion", BuildVersionStamp)
                        .SetProperty("InformationalVersion", BuildVersionStamp);
                }

                return s
                    .SetProject(controlAppProjectPath)
                    .SetConfiguration(Configuration.Release)
                    .SetRuntime("win-x64")
                    .SetOutput(publishOutput)
                    .SetSelfContained(false)
                    .SetProperty("PublishSingleFile", true)
                    .SetProperty("IncludeAllContentForSelfExtract", true);
            });

            Log.Information("ControlApp published to {PublishOutput}", publishOutput);
        });

    /// <summary>
    /// Download GitHub Actions build artifacts (ARM64, x64, x86) for a tagged run and optionally sign CABs, EXEs,
    /// and driver/XInput DLLs. Requires BuildVersion (a GitHub Actions run ID) and the "gh" CLI to be authenticated
    /// (run "gh auth login" once). Use --NoSigning to skip signing.
    /// </summary>
    [UsedImplicitly]
    public Target DownloadCiArtifacts => _ => _
        .Executes(() =>
        {
            if (string.IsNullOrWhiteSpace(BuildVersion))
            {
                throw new InvalidOperationException(
                    "DownloadCiArtifacts requires BuildVersion (a GitHub Actions run ID, see the \"Build\" workflow run URL).");
            }

            string artifactsDir = ResolvedArtifactsPath;
            Directory.CreateDirectory(artifactsDir);

            ProcessTasks.StartProcess("gh",
                    $"run download {BuildVersion} --repo nefarius/DsHidMini --dir \"{artifactsDir}\" --pattern \"dshidmini-*\"")
                .AssertZeroExitCode();

            if (!NoSigning)
            {
                string[] patterns = ["*.cab", "*.exe", "dshidmini.dll", "XInput1_3.dll"];
                List<string> existingFiles = patterns
                    .SelectMany(pattern => Directory.GetFiles(artifactsDir, pattern, SearchOption.AllDirectories))
                    .ToList();

                if (existingFiles.Count > 0)
                {
                    InvokeSignTool(
                        $"sign /v /n \"{SignCertName}\" /tr {SignTimestampUrl} /fd sha256 /td sha256 {string.Join(" ", existingFiles.Select(f => $"\"{f}\""))}");
                }
                else
                {
                    Log.Warning("No files found to sign under {ArtifactsDir}", artifactsDir);
                }
            }

            Log.Information("Helper job names for sign portal:");
            Log.Information("DsHidMini ARM64 v{BuildVersion} {Date:dd.MM.yyyy}", BuildVersion, DateTime.Now);
            Log.Information("DsHidMini x64 v{BuildVersion} {Date:dd.MM.yyyy}", BuildVersion, DateTime.Now);
        });

    /// <summary>
    /// Sign driver DLLs (append signature) under artifacts/drivers.
    /// </summary>
    [UsedImplicitly]
    public Target SignProductionBinaries => _ => _
        .Executes(() =>
        {
            string artifactsDir = ResolvedArtifactsPath;
            string[] patterns =
            [
                Path.Combine(artifactsDir, "drivers", "dshidmini_ARM64", "*.dll"),
                Path.Combine(artifactsDir, "drivers", "dshidmini_x64", "*.dll")
            ];
            List<string> files = new();
            foreach (string pattern in patterns)
            {
                string dir = Path.GetDirectoryName(pattern)!;
                if (Directory.Exists(dir))
                {
                    files.AddRange(Directory.GetFiles(dir, Path.GetFileName(pattern)));
                }
            }

            if (files.Count == 0)
            {
                Log.Warning("No driver DLLs found under {ArtifactsPath}", artifactsDir);
                return;
            }

            InvokeSignTool(
                $"sign /v /as /n \"{SignCertName}\" /tr {SignTimestampUrl} /fd sha256 /td sha256 {string.Join(" ", files.Select(f => $"\"{f}\""))}");
        });

    /// <summary>
    /// Build setup MSI and sign it. Requires SetupVersion (e.g. 3.0.0).
    /// </summary>
    [UsedImplicitly]
    public Target BuildSetup => _ => _
        .Executes(() =>
        {
            if (string.IsNullOrWhiteSpace(SetupVersion))
            {
                throw new InvalidOperationException("BuildSetup requires SetupVersion.");
            }

            AbsolutePath setupProject = RootDirectory / "setup" / "DsHidMini.Installer.csproj";
            if (!File.Exists(setupProject))
            {
                throw new InvalidOperationException($"Setup project not found at {setupProject}");
            }

            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(setupProject)
                .SetConfiguration(Configuration.Release)
                .SetProperty("SetupVersion", SetupVersion));

            string msiName = $"Nefarius_DsHidMini_Drivers_x64_arm64_v{SetupVersion}.msi";
            AbsolutePath msiInSetup = RootDirectory / "setup" / msiName;
            AbsolutePath msiInBin = RootDirectory / "setup" / "bin" / "Release" / "net48" / msiName;
            AbsolutePath msiPath = File.Exists(msiInSetup) ? msiInSetup : msiInBin;
            if (!File.Exists(msiPath))
            {
                throw new InvalidOperationException($"MSI not found: {msiInSetup} or {msiInBin}");
            }

            InvokeSignTool(
                $"sign /v /n \"{SignCertName}\" /tr {SignTimestampUrl} /fd sha256 /td sha256 \"{msiPath}\"");
        });

    /// Support plugins are available for:
    /// - JetBrains ReSharper        https://nuke.build/resharper
    /// - JetBrains Rider            https://nuke.build/rider
    /// - Microsoft VisualStudio     https://nuke.build/visualstudio
    /// - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);
}