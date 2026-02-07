using System;
using System.Collections.Generic;
using System.Linq;

using Nuke.Common;
using Nuke.Common.CI.AppVeyor;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
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

    Target Restore => _ => _
        .Executes(() =>
        {
        });

    Target BuildDmf => _ => _
        .Executes(() =>
        {
            Log.Information("DMF solution path: {DmfSolution}", DmfSolution);

            IEnumerable<(Configuration config, MSBuildTargetPlatform platform)> buildCombinations;
            if (IsLocalBuild)
            {
                var configs = new[] { Configuration.Debug, Configuration.Release };
                var platforms = new[] { MSBuildTargetPlatform.x64, (MSBuildTargetPlatform)"ARM64" };
                buildCombinations = configs.SelectMany(c => platforms.Select(p => (c, p)));
            }
            else
            {
                var appVeyorPlatform = AppVeyor.Instance?.Platform;
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
                buildCombinations = new[] { (Configuration, platform) };
            }

            foreach (var (config, platform) in buildCombinations)
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
                var settings = s
                    .SetTargetPath(Solution)
                    .SetTargets("Rebuild")
                    .SetConfiguration(Configuration)
                    .SetMaxCpuCount(Environment.ProcessorCount)
                    .SetNodeReuse(IsLocalBuild)
                    .SetVerbosity(MSBuildVerbosity.Minimal);

                // Aggressively silence C# warnings for local Nuke builds (nullability, CS8981, XML docs, etc.)
                if (IsLocalBuild)
                {
                    var noWarn = "CS0219;CS1587;CS1591;CS8600;CS8601;CS8602;CS8603;CS8604;CS8618;CS8619;CS8622;CS8625;CS8629;CS8765;CS8767;CS8981";
                    settings = settings.SetProperty("NoWarn", noWarn.Replace(";", "%3B"));
                }

                return settings;
            });
        });

    /// Support plugins are available for:
    /// - JetBrains ReSharper        https://nuke.build/resharper
    /// - JetBrains Rider            https://nuke.build/rider
    /// - Microsoft VisualStudio     https://nuke.build/visualstudio
    /// - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);
}