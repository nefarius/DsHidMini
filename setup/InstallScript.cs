#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;

using CliWrap;
using CliWrap.Buffered;

using Nefarius.DsHidMini.Setup.Dialogs;
using Nefarius.DsHidMini.Setup.Util;
using Nefarius.Utilities.DeviceManagement.Drivers;
using Nefarius.Utilities.DeviceManagement.Exceptions;
using Nefarius.Utilities.DeviceManagement.PnP;

using Newtonsoft.Json;

using WixSharp;
using WixSharp.CommonTasks;

using WixToolset.Dtf.WindowsInstaller;

using Assembly = System.Reflection.Assembly;
using File = WixSharp.File;

namespace Nefarius.DsHidMini.Setup;

internal class InstallScript
{
    public const string ProductName = "Nefarius DsHidMini Driver";
    public const string CustomActionManifestName = "ca-support-assemblies.txt";
    public const string ManifestsDir = "manifests";
    public const string EtwManifestName = "DsHidMini.man";

    public static Uri InstallationSuccessfulUrl = new("https://docs.nefarius.at/projects/DsHidMini/v3/Welcome/Installation-Successful/");

    /// <summary>
    /// Builds and emits the MSI installer for the Nefarius DsHidMini drivers and packaged artifacts.
    /// </summary>
    private static void Main()
    {
        RequireStagedInputs();

        if (string.IsNullOrWhiteSpace(BuildVariables.SetupVersion))
        {
            throw new InvalidOperationException(
                "SetupVersion is empty. Build the MSI through the GitHub Actions setup workflow.");
        }

        Version version = Version.Parse(BuildVariables.SetupVersion);
        const string driverPath = @"..\artifacts\drivers\x64\dshidmini.dll";
        Version driverVersion = Version.Parse(FileVersionInfo.GetVersionInfo(driverPath).FileVersion);

        const string filterPath = @"igfilter\nssmkig_x64\nssmkig.sys";
        Version filterVersion = Version.Parse(FileVersionInfo.GetVersionInfo(filterPath).FileVersion);

        const string nefconDir = @".\nefcon";

        Console.WriteLine($"Setup version: {version}");
        Console.WriteLine($"Driver version: {driverVersion}");
        Console.WriteLine($"Filter version: {filterVersion}");

        Feature driversFeature = new("DsHidMini Drivers", true, false)
        {
            Description = "Installs the Nefarius DsHidMini drivers for PS3 peripherals. " +
                          "This is a mandatory core component and can't be de-selected."
        };

        Feature bthPs3Feature = new("BthPS3 Wireless Drivers", false, true)
        {
            Id = "BthPS3Feature",
            Description = "When selected, downloads the latest version of the " +
                          "Nefarius BthPS3 Bluetooth Drivers for wireless connectivity. " +
                          "You need to go through the BthPS3 installation AFTER this installation has finished."
        };

        Feature postInstallArticleFeature = new("Open post-installation article", true, true)
        {
            Id = "PostInstArticle",
            Description = "When setup has finished successfully, open the post-installation web article."
        };

        driversFeature.Add(bthPs3Feature);
        driversFeature.Add(postInstallArticleFeature);
        driversFeature.Display = FeatureDisplay.expand;

        string[] customActionAssemblies = GetCustomActionSupportAssemblies();

        ManagedProject project = new(ProductName,
            new InstallDir(@"%ProgramFiles%\Nefarius Software Solutions\DsHidMini",
                new Dir(driversFeature, "nefcon")
                {
                    Files = new DirFiles(driversFeature, "*.*").GetFiles(nefconDir),
                    Dirs = GetSubDirectories(driversFeature, nefconDir)
                },
                new Dir(driversFeature, "drivers",
                    new Files(driversFeature, @"..\artifacts\drivers\*.*"),
                    new Files(driversFeature, @".\igfilter\*.*")
                ),
                new Dir(driversFeature, ManifestsDir,
                    new File(driversFeature, $@"..\driver\{EtwManifestName}")
                ),
                new File(driversFeature, "nefarius_DsHidMini_Updater.exe"),
                new File(driversFeature, @"..\artifacts\bin\ControlApp.exe")
            ),
            new Dir(driversFeature, @"%ProgramMenu%\Nefarius Software Solutions\DsHidMini",
                new ExeFileShortcut("DsHidMini Control App", "[INSTALLDIR]ControlApp.exe", "")),
            new ManagedAction(CustomActions.CheckDotNetRuntime, Return.check,
                When.Before,
                Step.LaunchConditions,
                Condition.NOT_Installed)
            {
                RefAssemblies = customActionAssemblies
            },
            new ElevatedManagedAction(CustomActions.InstallDrivers, Return.check,
                When.After,
                Step.InstallFiles,
                Condition.NOT_Installed)
            {
                RefAssemblies = customActionAssemblies
            },
            new ElevatedManagedAction(CustomActions.InstallManifest, Return.check,
                When.After,
                Step.InstallFiles,
                Condition.NOT_Installed)
            {
                RefAssemblies = customActionAssemblies
            },
            new ElevatedManagedAction(CustomActions.UninstallManifest, Return.check,
                When.Before,
                Step.RemoveFiles,
                new Condition("REMOVE=\"ALL\""))
            {
                RefAssemblies = customActionAssemblies
            },
            new Error("9000",
                "Driver installation succeeded but a reboot is required to be fully operational. " +
                "After the setup is finished, please reboot the system before using the software."),
            new Error("9001",
                "The .NET 10 Desktop Runtime (x64) is required by DsHidMini Control App. " +
                "Please download and install it from https://dotnet.microsoft.com/download/dotnet/10.0 " +
                "and then re-run this installer."),
            new ManagedAction(CustomActions.InstallBthPS3, Return.check,
                When.After,
                Step.InstallFinalize,
                Condition.NOT_Installed)
            {
                RefAssemblies = customActionAssemblies
            },
            new ManagedAction(CustomActions.RegisterUpdater, Return.check,
                When.After,
                Step.InstallFinalize,
                Condition.NOT_Installed)
            {
                RefAssemblies = customActionAssemblies
            },
            new ManagedAction(CustomActions.OpenArticle, Return.check,
                When.After,
                Step.InstallFinalize,
                Condition.NOT_Installed)
            {
                RefAssemblies = customActionAssemblies
            },
            // Only a full uninstall should unregister the updater. Conditioning this on
            // Installed would also fire on repair and modify, where RegisterUpdater does not
            // run, leaving the updater deregistered with nothing putting it back.
            new ManagedAction(CustomActions.DeregisterUpdater, Return.check,
                When.Before,
                Step.RemoveFiles,
                new Condition("REMOVE=\"ALL\""))
            {
                RefAssemblies = customActionAssemblies
            },
            new RegKey(driversFeature, RegistryHive.LocalMachine,
                $@"Software\Nefarius Software Solutions e.U.\{ProductName}",
                new RegValue("Path", "[INSTALLDIR]") { Win64 = true },
                new RegValue("Version", version.ToString()) { Win64 = true },
                new RegValue("DriverVersion", driverVersion.ToString()) { Win64 = true },
                new RegValue("FilterVersion", filterVersion.ToString()) { Win64 = true }
            ) { Win64 = true }
        )
        {
            ManagedUI = new ManagedUI(),
            OutFileName = $"Nefarius_DsHidMini_Drivers_x64_arm64_v{version}",
            Version = version,
            Platform = Platform.x64,
            // Entities without an explicit feature would otherwise land in WixSharp's
            // synthetic "Complete" feature, which appears as a second root in the feature
            // tree and leaves the mandatory drivers feature deselected.
            DefaultFeature = driversFeature,
            GUID = new Guid("25784100-B9AA-4205-8D54-CA53717F6AC5"),
            LicenceFile = "EULA.rtf",
            WildCardDedup = Project.UniqueFileNameDedup,
            BannerImage = "DsHidMini.dialog_banner.bmp",
            BackgroundImage = "DsHidMini.dialog_background.bmp",
            CAConfigFile = "CustomActions.config"
        };

        // The 9000 message tells the user to reboot manually, so MSI must never schedule or
        // prompt for a reboot itself. This applies to the whole session, including the
        // removal of an older version.
        project.AddProperty(new Property("REBOOT", "ReallySuppress"));

        project.MajorUpgrade = new MajorUpgrade
        {
            Schedule = UpgradeSchedule.afterInstallInitialize,
            DowngradeErrorMessage = "A later version of [ProductName] is already installed. Setup will now exit.",
            AllowSameVersionUpgrades = true
        };

        project.ManagedUI.InstallDialogs.Add<WelcomeDialog>()
            .Add<LicenceDialog>()
            .Add<FeaturesDialog>()
            .Add<ProgressDialog>()
            .Add<OnlineDocumentationDialog>()
            .Add<ExitDialog>();

        project.ManagedUI.ModifyDialogs.Add<MaintenanceTypeDialog>()
            .Add<FeaturesDialog>()
            .Add<ProgressDialog>()
            .Add<ExitDialog>();

        project.DefaultRefAssemblies.AddRange(customActionAssemblies);

        project.AfterInstall += ProjectOnAfterInstall;

        project.ControlPanelInfo.ProductIcon = @"..\assets\FireShock.ico";
        project.ControlPanelInfo.Manufacturer = "Nefarius Software Solutions e.U.";
        project.ControlPanelInfo.HelpLink = "https://docs.nefarius.at/Community-Support/";
        project.ControlPanelInfo.UrlInfoAbout = "https://github.com/nefarius/DsHidMini";
        project.ControlPanelInfo.NoModify = true;

        project.ResolveWildCards();

        WixTools.WixDtfPackages = new[]
        {
            ("wixtoolset.dtf.customaction", "5.0.2"),
            ("wixtoolset.dtf.windowsinstaller", "4.0.6"),
            ("wixtoolset.heat", "*"),
            ("wixtoolset.mba.core", "*")
        };
        WixTools.RestoreDtfPackages();

        string sfxCa = WixTools.SfxCAFor(true);
        FileVersionInfo sfxCaVersion = FileVersionInfo.GetVersionInfo(sfxCa);
        Console.WriteLine($"SfxCA.dll: {sfxCaVersion.FileVersion} ({sfxCa})");
        if (sfxCaVersion.FileMajorPart < 5)
        {
            throw new InvalidOperationException(
                $"SfxCA.dll {sfxCaVersion.FileVersion} at '{sfxCa}' predates the WiX v5 temp-folder fix; " +
                "non-elevated installs would fail with 1603.");
        }

        project.BuildMsi();
    }

    /// <summary>
    ///     Assemblies MakeSfxCA must pack beside the deferred custom-action host: the
    ///     transitive reference closure of this assembly, restricted to files that ship
    ///     in its own output directory.
    /// </summary>
    private static string[] GetCustomActionSupportAssemblies()
    {
        Assembly root = typeof(CustomActions).Assembly;
        string directory = Path.GetDirectoryName(root.Location);
        if (string.IsNullOrEmpty(directory))
        {
            throw new InvalidOperationException(
                "Cannot resolve the custom-action output directory; Assembly.Location is empty.");
        }

        SortedDictionary<string, string> resolved = new(StringComparer.OrdinalIgnoreCase);
        Queue<Assembly> pending = new();
        HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase);

        pending.Enqueue(root);
        visited.Add(root.GetName().Name);

        while (pending.Count > 0)
        {
            foreach (AssemblyName reference in pending.Dequeue().GetReferencedAssemblies())
            {
                if (!visited.Add(reference.Name))
                {
                    continue;
                }

                if (IsWixAssembly(reference.Name))
                {
                    continue;
                }

                string path = new[] { ".dll", ".exe" }
                    .Select(extension => Path.Combine(directory, reference.Name + extension))
                    .FirstOrDefault(System.IO.File.Exists);

                if (path is null)
                {
                    if (IsFrameworkAssembly(reference))
                    {
                        continue;
                    }

                    throw new InvalidOperationException(
                        $"Custom-action dependency '{reference.FullName}' was not found in " +
                        $"'{directory}' and is not a framework assembly. Add it to " +
                        "DsHidMini.Installer.csproj so MakeSfxCA can pack it; a deferred custom " +
                        "action would otherwise fail at runtime with FileNotFoundException.");
                }

                resolved[reference.Name] = path;
                pending.Enqueue(Assembly.LoadFrom(path));
            }
        }

        string[] assemblies = resolved.Values.ToArray();

        Console.WriteLine($"Custom-action support assemblies: {assemblies.Length}");
        foreach (string assembly in assemblies)
        {
            Console.WriteLine($"  {Path.GetFileName(assembly)}");
        }

        WriteCustomActionManifest(assemblies);

        return assemblies;
    }

    private static bool IsWixAssembly(string name)
    {
        return name.StartsWith("WixSharp", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("WixToolset.", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFrameworkAssembly(AssemblyName reference)
    {
        try
        {
            return Assembly.ReflectionOnlyLoad(reference.FullName).GlobalAssemblyCache;
        }
        catch (Exception exception) when (exception is IOException ||
                                          exception is BadImageFormatException)
        {
            return false;
        }
    }

    private static void WriteCustomActionManifest(string[] assemblies)
    {
        string path = Path.Combine("obj", CustomActionManifestName);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        System.IO.File.WriteAllLines(path, assemblies.Select(Path.GetFileName));
        Console.WriteLine($"Custom-action manifest: {Path.GetFullPath(path)}");
    }

    private static Dir[] GetSubDirectories(Feature feature, string directory)
    {
        return Directory.GetDirectories(directory)
            .Select(subDirectory =>
            {
                string name = Path.GetFileName(subDirectory);
                return new Dir(feature, name, new Files(feature, Path.Combine(subDirectory, "*.*")));
            })
            .ToArray();
    }

    private static void RequireStagedInputs()
    {
        string[] required =
        [
            @"..\artifacts\drivers\dshidmini.inf",
            @"..\artifacts\drivers\dshidmini.cat",
            @"..\artifacts\drivers\x64\dshidmini.dll",
            @"..\artifacts\drivers\ARM64\dshidmini.dll",
            @"igfilter\nssmkig_x64\igfilter.inf",
            @"igfilter\nssmkig_x64\nssmkig.sys",
            @"igfilter\nssmkig_ARM64\igfilter.inf",
            @"igfilter\nssmkig_ARM64\nssmkig.sys",
            @"..\artifacts\bin\ControlApp.exe",
            @"nefcon\x64\nefconc.exe",
            @"nefcon\ARM64\nefconc.exe",
            @"nefarius_DsHidMini_Updater.exe",
            $@"..\driver\{EtwManifestName}"
        ];

        string[] missing = required.Where(path => !System.IO.File.Exists(path)).ToArray();
        if (missing.Length == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "MSI inputs are missing. Build the MSI through the GitHub Actions setup workflow." +
            Environment.NewLine +
            string.Join(Environment.NewLine, missing.Select(path => "Missing: " + path)));
    }

    private static void ProjectOnAfterInstall(SetupEventArgs e)
    {
        if (e.IsUninstalling)
        {
            CustomActions.UninstallDrivers(e.Session);
        }
    }
}

public static class CustomActions
{
    static CustomActions()
    {
        CustomActionAssemblyResolver.Register();
    }

    [CustomAction]
    public static ActionResult CheckDotNetRuntime(Session session)
    {
        const int requiredMajor = 10;
        string runtimeDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "dotnet", "shared", "Microsoft.WindowsDesktop.App");

        session.Log($"Probing .NET Desktop Runtime directory: {runtimeDir}");

        try
        {
            if (Directory.Exists(runtimeDir))
            {
                foreach (string versionDir in Directory.GetDirectories(runtimeDir))
                {
                    string dirName = Path.GetFileName(versionDir);
                    int dashIndex = dirName.IndexOf('-');
                    string numericPart = dashIndex >= 0 ? dirName.Substring(0, dashIndex) : dirName;
                    if (Version.TryParse(numericPart, out Version? installedVersion) &&
                        installedVersion.Major >= requiredMajor)
                    {
                        session.Log($".NET Desktop Runtime {installedVersion} found - prerequisite satisfied.");
                        return ActionResult.Success;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            session.Log($"Failed to probe .NET {requiredMajor} Desktop Runtime directory: {ex}");
        }

        session.Log($".NET {requiredMajor} Desktop Runtime not found, aborting installation.");

        Record record = new(1);
        record[1] = "9001";
        session.Message(
            InstallMessage.User | (InstallMessage)MessageButtons.OK | (InstallMessage)MessageIcon.Error,
            record);

        return ActionResult.Failure;
    }

    [CustomAction]
    public static ActionResult InstallDrivers(Session session)
    {
        bool rebootRequired = UninstallDrivers(session);

        DirectoryInfo installDir = new(session.Property("INSTALLDIR"));
        session.Log($"installDir = {installDir}");
        string driversDir = Path.Combine(installDir.FullName, "drivers");
        session.Log($"driversDir = {driversDir}");
        string nefconDir = Path.Combine(installDir.FullName, "nefcon");
        session.Log($"nefconDir = {nefconDir}");
        string archShortName = ArchitectureInfo.PlatformShortName;
        session.Log($"archShortName = {archShortName}");

        string nefconcPath = Path.Combine(nefconDir, archShortName, "nefconc.exe");
        session.Log($"nefconcPath = {nefconcPath}");

        string igfilterDriverDir = Path.Combine(driversDir, $"nssmkig_{archShortName}");
        session.Log($"igfilterDriverDir = {igfilterDriverDir}");

        string dshidminiInfPath = Path.Combine(driversDir, "dshidmini.inf");
        session.Log($"dshidminiInfPath = {dshidminiInfPath}");
        string igfilterInfPath = Path.Combine(igfilterDriverDir, "igfilter.inf");
        session.Log($"igfilterInfPath = {igfilterInfPath}");

        BufferedCommandResult? result = Cli.Wrap(nefconcPath)
            .WithArguments(builder => builder
                .Add("--inf-default-install")
                .Add("--inf-path")
                .Add(igfilterInfPath)
            )
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync()
            .GetAwaiter()
            .GetResult();

        session.Log($"igfilter command stdout: {result.StandardOutput}");
        session.Log($"igfilter command stderr: {result.StandardError}");

        if (result?.ExitCode == 3010)
        {
            rebootRequired = true;
        }

        if (result?.ExitCode != 0 && result?.ExitCode != 3010)
        {
            session.Log(
                $"Filter installer failed with exit code: {result?.ExitCode}, message: {Win32Exception.GetMessageFor(result?.ExitCode)}");

            return ActionResult.Failure;
        }

        if (!Devcon.Install(dshidminiInfPath, out bool dshidminiRebootRequired))
        {
            int error = Marshal.GetLastWin32Error();
            session.Log(
                $"Driver installation failed with win32 error: {error}, message: {Win32Exception.GetMessageFor(error)}");

            return ActionResult.Failure;
        }

        if (dshidminiRebootRequired)
        {
            rebootRequired = true;
        }

        Devcon.Refresh();

        if (rebootRequired)
        {
            Record record = new(1);
            record[1] = "9000";

            session.Message(
                InstallMessage.User | (InstallMessage)MessageButtons.OK | (InstallMessage)MessageIcon.Information,
                record);
        }

        return ActionResult.Success;
    }

    [CustomAction]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static ActionResult InstallBthPS3(Session session)
    {
        if (!session.IsFeatureEnabled("BthPS3Feature"))
        {
            return ActionResult.Success;
        }

        try
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using WebClient client = new();
            client.Headers.Add("X-Vicius-OS-Architecture", ArchitectureInfo.PlatformShortName);

            string json = client.DownloadString("https://vicius.api.nefarius.systems/api/nefarius/BthPS3/updates.json");
            UpdateResponse? response = JsonConvert.DeserializeObject<UpdateResponse>(json);
            Release? latestRelease = response!.Releases.OrderByDescending(r => r.Version).First();
            Uri downloadUrl = new(latestRelease.DownloadUrl);
            session.Log($"BthPS3 download URL: {downloadUrl}");

            CommandResult? result = Cli.Wrap("explorer")
                .WithArguments(downloadUrl.ToString())
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync()
                .GetAwaiter()
                .GetResult();

            session.Log(
                $"Download URL launch {(result.IsSuccess ? "succeeded" : "failed")}, exit code: {result.ExitCode}");

            return ActionResult.Success;
        }
        catch (Exception ex)
        {
            session.Log($"BthPS3 download/install failed: {ex}");
        }

        return ActionResult.Success;
    }

    [CustomAction]
    public static ActionResult OpenArticle(Session session)
    {
        // MSI UILevel is reported as silent for Embedded/ManagedUI even during a full
        // wizard run. WIXSHARP_MANAGED_UI_HANDLE is only set when the ManagedUI window
        // is actually shown; it stays empty for reduced/basic/suppressed execution.
        string managedUiHandle = session.Property("WIXSHARP_MANAGED_UI_HANDLE");
        bool articleFeatureEnabled = session.IsFeatureEnabled("PostInstArticle");
        bool shouldLaunch = OpenArticleDecision.ShouldLaunch(managedUiHandle, articleFeatureEnabled);

        session.Log(
            $"{nameof(OpenArticle)} - WIXSHARP_MANAGED_UI_HANDLE='{managedUiHandle}', " +
            $"managedUiDisplayed={!string.IsNullOrWhiteSpace(managedUiHandle)}, PostInstArticle={articleFeatureEnabled}");

        if (!shouldLaunch)
        {
            session.Log($"{nameof(OpenArticle)} - skipping launch; feature deselected in full UI");
            return ActionResult.Success;
        }

        try
        {
            session.Log($"{nameof(OpenArticle)} - launching post-installation article");
            Process.Start(InstallScript.InstallationSuccessfulUrl.ToString());
        }
        catch (Exception ex)
        {
            session.Log($"Spawning article process failed with {ex}");
        }

        return ActionResult.Success;
    }

    [CustomAction]
    public static ActionResult RegisterUpdater(Session session)
    {
        DirectoryInfo installDir = new(session.Property("INSTALLDIR"));
        string updaterPath = Path.Combine(installDir.FullName, "nefarius_DsHidMini_Updater.exe");

        CommandResult? result = Cli.Wrap(updaterPath)
            .WithArguments(builder => builder
                .Add("--install")
                .Add("--silent")
            )
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync()
            .GetAwaiter()
            .GetResult();

        session.Log(
            $"Updater registration {(result.IsSuccess ? "succeeded" : "failed")}, exit code: {result.ExitCode}");

        return ActionResult.Success;
    }

    [CustomAction]
    public static ActionResult DeregisterUpdater(Session session)
    {
        try
        {
            DirectoryInfo installDir = new(session.Property("INSTALLDIR"));
            string updaterPath = Path.Combine(installDir.FullName, "nefarius_DsHidMini_Updater.exe");

            CommandResult? result = Cli.Wrap(updaterPath)
                .WithArguments(builder => builder
                    .Add("--uninstall")
                    .Add("--silent")
                )
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync()
                .GetAwaiter()
                .GetResult();

            session.Log(
                $"Updater de-registration {(result.IsSuccess ? "succeeded" : "failed")}, exit code: {result.ExitCode}");

            return ActionResult.Success;
        }
        catch
        {
            return ActionResult.Success;
        }
    }

    /// <summary>
    ///     Registers the DsHidMini ETW instrumentation manifest.
    /// </summary>
    /// <remarks>Requires elevated permissions.</remarks>
    [CustomAction]
    public static ActionResult InstallManifest(Session session)
    {
        DirectoryInfo installDir = new(session.Property("INSTALLDIR"));
        string manifest = Path.Combine(installDir.FullName, InstallScript.ManifestsDir, InstallScript.EtwManifestName);

        CommandResult? result = Cli.Wrap("wevtutil")
            .WithArguments(builder => builder
                .Add("im")
                .Add(manifest)
            )
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync()
            .GetAwaiter()
            .GetResult();

        session.Log(
            $"DsHidMini manifest import {(result.IsSuccess ? "succeeded" : "failed")}, " +
            $"exit code: {result.ExitCode}");

        return ActionResult.Success;
    }

    /// <summary>
    ///     Unregisters the DsHidMini ETW instrumentation manifest.
    /// </summary>
    /// <remarks>Requires elevated permissions.</remarks>
    [CustomAction]
    public static ActionResult UninstallManifest(Session session)
    {
        DirectoryInfo installDir = new(session.Property("INSTALLDIR"));
        string manifest = Path.Combine(installDir.FullName, InstallScript.ManifestsDir, InstallScript.EtwManifestName);

        CommandResult? result = Cli.Wrap("wevtutil")
            .WithArguments(builder => builder
                .Add("um")
                .Add(manifest)
            )
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync()
            .GetAwaiter()
            .GetResult();

        session.Log(
            $"DsHidMini manifest removal {(result.IsSuccess ? "succeeded" : "failed")}, " +
            $"exit code: {result.ExitCode}");

        return ActionResult.Success;
    }

    public static bool UninstallDrivers(Session session)
    {
        List<string> allDriverPackages = DriverStore.ExistingDrivers.ToList();

        foreach (string driverPackage in allDriverPackages.Where(p =>
                     p.IndexOf("dshidmini.inf", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            try
            {
                DriverStore.RemoveDriver(driverPackage);
            }
            catch (Exception ex)
            {
                session.Log($"Removal of dshidmini package {driverPackage} failed with error {ex}");
            }
        }

        foreach (string driverPackage in allDriverPackages.Where(p =>
                     p.IndexOf("igfilter.inf", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            try
            {
                DriverStore.RemoveDriver(driverPackage);
            }
            catch (Exception ex)
            {
                session.Log($"Removal of igfilter package {driverPackage} failed with error {ex}");
            }
        }

        foreach (string driverPackage in allDriverPackages.Where(p =>
                     p.IndexOf("ds3controller.inf", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     p.IndexOf("ds3controller_", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            try
            {
                DriverStore.RemoveDriver(driverPackage);
            }
            catch (Exception ex)
            {
                session.Log($"Removal of SCP DS3 package {driverPackage} failed with error {ex}");
            }
        }

        foreach (string driverPackage in allDriverPackages.Where(p =>
                     p.IndexOf("fireshock.inf", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            try
            {
                DriverStore.RemoveDriver(driverPackage);
            }
            catch (Exception ex)
            {
                session.Log($"Removal of fireshock package {driverPackage} failed with error {ex}");
            }
        }

        foreach (string driverPackage in allDriverPackages.Where(p =>
                     p.IndexOf("sixaxis.inf", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            try
            {
                DriverStore.RemoveDriver(driverPackage);
            }
            catch (Exception ex)
            {
                session.Log($"Removal of sixaxis package {driverPackage} failed with error {ex}");
            }
        }

        bool rebootRequired = false;
        int instance = 0;
        while (Devcon.FindByInterfaceGuid(DsHidMiniDriver.DeviceInterfaceGuid, out PnPDevice device, instance++, false))
        {
            try
            {
                device.Uninstall(out bool reboot);

                if (reboot)
                {
                    rebootRequired = true;
                }
            }
            catch (Exception ex)
            {
                session.Log($"Removal of device instance {device} failed with error {ex}");
            }
        }

        return rebootRequired;
    }
}
