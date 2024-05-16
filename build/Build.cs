using System;

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

    AbsolutePath DmfSolution => "C:/projects/DMF/Dmf.sln";

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

            if (AppVeyor.Instance.Platform == MSBuildTargetPlatform.x86 ||
                AppVeyor.Instance.Platform == MSBuildTargetPlatform.Win32)
            {
                Log.Warning("DMF dropped 32-Bit support, skipping build");
                return;
            }

            MSBuildTargetPlatform platform = AppVeyor.Instance.Platform switch
            {
                "ARM64" => "ARM64",
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

            bool enableTelemetry =
                bool.TryParse(Environment.GetEnvironmentVariable("SCPLIB_ENABLE_TELEMETRY"), out bool isSet) && isSet;

            MSBuildTasks.MSBuild(s =>
            {
                s
                    .SetTargetPath(Solution)
                    .SetTargets("Rebuild")
                    .SetConfiguration(Configuration)
                    .SetMaxCpuCount(Environment.ProcessorCount)
                    .SetNodeReuse(IsLocalBuild);

                if (enableTelemetry)
                {
                    // enables building with OTEL in XInputBridge
                    s.SetProperty("SCPLIB_ENABLE_TELEMETRY", true);
                }

                return s;
            });
        });

    /// Support plugins are available for:
    /// - JetBrains ReSharper        https://nuke.build/resharper
    /// - JetBrains Rider            https://nuke.build/rider
    /// - Microsoft VisualStudio     https://nuke.build/visualstudio
    /// - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);
}