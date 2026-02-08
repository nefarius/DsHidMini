using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

using JetBrains.Annotations;

using Nuke.Common;
using Nuke.Common.CI.AppVeyor;
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

    [Parameter("Build version or branch for DownloadAppVeyorArtifacts (AppVeyor artifact download)")]
    readonly string BuildVersion = "";

    [Parameter("AppVeyor API token for DownloadAppVeyorArtifacts artifact download")]
    readonly string Token = "";

    [Parameter("Output path for DownloadAppVeyorArtifacts artifacts. Default: ./artifacts")]
    readonly string ArtifactsPath = "./artifacts";

    [Parameter("Skip signing in DownloadAppVeyorArtifacts")]
    readonly bool NoSigning;

    [Parameter("Setup version for BuildSetup (e.g. 3.0.0)")]
    readonly string SetupVersion = "";

    [Parameter("Path to signtool.exe. When not set, Nefarius.Tools.WDKWhere is used to run signtool.")]
    readonly string SignToolPath = "";

    [NuGetPackage("Nefarius.Tools.WDKWhere", "wdkwhere.dll", Framework = "net8.0")]
    readonly Tool WdkWhere;

    const string AppVeyorApiUrl = "https://ci.appveyor.com/api";
    const string SignTimestampUrl = "http://timestamp.digicert.com";
    const string SignCertName = "Nefarius Software Solutions e.U.";

    AbsolutePath DmfSolution => IsLocalBuild
        ? Solution.Directory / "DMF/Dmf.sln"
        : "C:/projects/DMF/Dmf.sln";

    AbsolutePath ResolvedArtifactsPath => (AbsolutePath)Path.GetFullPath(Path.Combine(RootDirectory, ArtifactsPath));

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
                string appVeyorPlatform = AppVeyor.Instance?.Platform;
                if (appVeyorPlatform == MSBuildTargetPlatform.x86 ||
                    appVeyorPlatform == MSBuildTargetPlatform.Win32)
                {
                    Log.Warning("DMF dropped 32-Bit support, skipping build");
                    return;
                }

                MSBuildTargetPlatform platform = appVeyorPlatform switch
                {
                    "ARM64" => "ARM64",
                    _ => MSBuildTargetPlatform.x64
                };
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
    /// Download AppVeyor build artifacts (ARM64, x64, x86) and optionally sign CABs, EXEs, and driver/XInput DLLs.
    /// Requires BuildVersion and Token. Use --NoSigning to skip signing.
    /// </summary>
    [UsedImplicitly]
    public Target DownloadAppVeyorArtifacts => _ => _
        .Executes(() =>
        {
            if (string.IsNullOrWhiteSpace(BuildVersion) || string.IsNullOrWhiteSpace(Token))
            {
                throw new InvalidOperationException("DownloadAppVeyorArtifacts requires BuildVersion and Token.");
            }

            string artifactsDir = ResolvedArtifactsPath;
            Directory.CreateDirectory(artifactsDir);

            using HttpClient http = new();
            http.DefaultRequestHeaders.Add("Authorization", "Bearer " + Token);

            string projectUri = $"{AppVeyorApiUrl}/projects/nefarius/DsHidMini/build/{BuildVersion}";
            Log.Information("Fetching build info: {Uri}", projectUri);
            string json = http.GetStringAsync(projectUri).GetAwaiter().GetResult();
            using (JsonDocument buildDoc = JsonDocument.Parse(json))
            {
                JsonElement build = buildDoc.RootElement.GetProperty("build");
                JsonElement jobs = build.GetProperty("jobs");

                string[] jobNames = ["Platform: ARM64", "Platform: x64", "Platform: x86"];
                foreach (string jobName in jobNames)
                {
                    string? jobId = null;
                    foreach (JsonElement job in jobs.EnumerateArray())
                    {
                        if (job.GetProperty("name").GetString() != jobName)
                            continue;
                        JsonElement jobIdEl = job.GetProperty("jobId");
                        jobId = jobIdEl.ValueKind == JsonValueKind.String
                            ? jobIdEl.GetString()!
                            : jobIdEl.GetInt32().ToString();
                        break;
                    }

                    if (string.IsNullOrEmpty(jobId))
                    {
                        Log.Warning("Job not found: {JobName}", jobName);
                        continue;
                    }

                    string artifactsListUri = $"{AppVeyorApiUrl}/buildjobs/{jobId}/artifacts";
                    string listJson = http.GetStringAsync(artifactsListUri).GetAwaiter().GetResult();
                    using JsonDocument listDoc = JsonDocument.Parse(listJson);
                    foreach (JsonElement artifact in listDoc.RootElement.EnumerateArray())
                    {
                        string fileName = artifact.GetProperty("fileName").GetString()!;
                        string localPath = Path.Combine(artifactsDir,
                            fileName.Replace('/', Path.DirectorySeparatorChar));
                        string localPathUnescaped = Uri.UnescapeDataString(localPath);
                        string downloadUri =
                            $"{AppVeyorApiUrl}/buildjobs/{jobId}/artifacts/{artifact.GetProperty("fileName").GetString()}";
                        Directory.CreateDirectory(Path.GetDirectoryName(localPathUnescaped)!);
                        byte[] bytes = http.GetByteArrayAsync(downloadUri).GetAwaiter().GetResult();
                        File.WriteAllBytes(localPathUnescaped, bytes);
                        Log.Information("Downloaded {File}", localPathUnescaped);
                    }
                }
            }

            if (!NoSigning)
            {
                string[] files =
                [
                    Path.Combine(artifactsDir, "disk1", "*.cab"), Path.Combine(artifactsDir, "bin", "*.exe"),
                    Path.Combine(artifactsDir, "bin", "ARM64", "dshidmini", "dshidmini.dll"),
                    Path.Combine(artifactsDir, "bin", "x64", "dshidmini", "dshidmini.dll"),
                    Path.Combine(artifactsDir, "bin", "x64", "XInput1_3.dll"),
                    Path.Combine(artifactsDir, "bin", "ARM64", "XInput1_3.dll"),
                    Path.Combine(artifactsDir, "bin", "x86", "XInput1_3.dll")
                ];
                List<string> existingFiles = new();
                foreach (string pattern in files)
                {
                    string dir = Path.GetDirectoryName(pattern)!;
                    string search = Path.GetFileName(pattern);
                    if (search.Contains('*'))
                    {
                        if (Directory.Exists(dir))
                        {
                            existingFiles.AddRange(Directory.GetFiles(dir, search));
                        }
                    }
                    else if (File.Exists(pattern))
                    {
                        existingFiles.Add(pattern);
                    }
                }
                
                if (existingFiles.Count > 0)
                {
                    InvokeSignTool(
                        $"sign /v /n \"{SignCertName}\" /tr {SignTimestampUrl} /fd sha256 /td sha256 {string.Join(" ", existingFiles.Select(f => $"\"{f}\""))}");
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