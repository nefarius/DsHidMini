using System;
using System.Linq;

using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AppVeyor;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;

using Serilog;

using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution]
    readonly Solution Solution;

    AbsolutePath DmfSolution => (AbsolutePath)"C:/projects/DMF/Dmf.sln";
    
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
            if (IsLocalBuild)
            {
                return;
            }

            Log.Information("DMF solution path: {DmfSolution}", DmfSolution);

            if (AppVeyor.Instance.Platform == MSBuildTargetPlatform.Win32)
            {
                Serilog.Log.Warning("DMF dropped 32-Bit support, skipping build");
                return;
            }

            MSBuildTargetPlatform platform = AppVeyor.Instance.Platform switch
            {
                "ARM64" => (MSBuildTargetPlatform)"ARM64",
                _ => MSBuildTargetPlatform.x64
            };

            MSBuildTasks.MSBuild(s => s
                .SetTargetPath(DmfSolution)
                .SetTargets("Build")
                .SetConfiguration(Configuration)
                .SetTargetPlatform(platform)
                .SetMaxCpuCount(Environment.ProcessorCount)
                .SetNodeReuse(IsLocalBuild));
        });

    Target Compile => _ => _
        .DependsOn(BuildDmf)
        .Executes(() =>
        {
            Logging.Level = LogLevel.Normal;

            MSBuildTasks.MSBuild(s => s
                .SetTargetPath(Solution)
                .SetTargets("Rebuild")
                .SetConfiguration(Configuration)
                // enables building with OTEL in XInputBridge
                .SetProperty("SCPLIB_ENABLE_TELEMETRY", true)
                .SetMaxCpuCount(Environment.ProcessorCount)
                .SetNodeReuse(IsLocalBuild));
        });
}