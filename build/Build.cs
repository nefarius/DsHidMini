using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Nuke.Common;
using Nuke.Common.CI.AppVeyor;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;

using Serilog;

class Build : NukeBuild
{
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution]
    readonly Solution Solution;

    AbsolutePath DmfSolution => IsLocalBuild
        ? Solution.Directory / "DMF/Dmf.sln"
        : "C:/projects/DMF/Dmf.sln";

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

    /// Support plugins are available for:
    /// - JetBrains ReSharper        https://nuke.build/resharper
    /// - JetBrains Rider            https://nuke.build/rider
    /// - Microsoft VisualStudio     https://nuke.build/visualstudio
    /// - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);
}