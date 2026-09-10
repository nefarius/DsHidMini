using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

static class ReleasePipelineTests
{
    public static void Run()
    {
        TestMetadataRoundTrip();
        TestArrangeDownloadedArtifacts();
        TestStageMicrosoftDrivers();
        TestUniquePackageDetection();
        TestIngestFromDirectoryAndZip();
        TestIgfilterStaging();
        TestSetupValidationWithoutSignatures();
        TestSignatureParser();
        TestSetupMsiContractAcceptsControlAppPayload();
        TestSetupMsiContractRejectsMissingControlApp();
        TestSetupMsiContractRejectsMissingShortcut();
        TestSetupMsiContractRejectsMissingDotNetPrerequisite();
        TestSetupMsiContractRejectsNearMatchAndUiOnlyRuntimeAction();
        Console.WriteLine("ReleasePipeline fixture tests passed");
    }

    static void TestMetadataRoundTrip()
    {
        using TempScope scope = new();
        ReleaseMetadata metadata = SampleMetadata();
        string path = Path.Combine(scope.Root, ReleaseStaging.MetadataFileName);
        ReleaseStaging.WriteMetadata(path, metadata);
        ReleaseMetadata loaded = ReleaseStaging.ReadMetadata(path);
        AssertEqual(loaded.Tag, "v3.6.0", nameof(TestMetadataRoundTrip));
        AssertEqual(loaded.DriverVersion, "3.6.0.2145", nameof(TestMetadataRoundTrip));

        metadata.Tag = "v3.6.0.1";
        path = Path.Combine(scope.Root, "bad.json");
        ReleaseStaging.WriteMetadata(path, metadata);
        AssertThrows(() => ReleaseStaging.ReadMetadata(path), "four-part metadata tag");

        metadata.Tag = "v3.6.0";
        metadata.Files = null;
        path = Path.Combine(scope.Root, "no-cab.json");
        ReleaseStaging.WriteMetadata(path, metadata);
        AssertThrows(() => ReleaseStaging.ReadMetadata(path), "missing partnerCab");
    }

    static void TestArrangeDownloadedArtifacts()
    {
        using TempScope scope = new();
        string download = Path.Combine(scope.Root, "download");
        string artifacts = Path.Combine(scope.Root, "artifacts");
        ReleaseMetadata metadata = SampleMetadata();
        string cab = Path.Combine(download, "dshidmini-partner-submission", metadata.Files.PartnerCab.Name);
        Directory.CreateDirectory(Path.GetDirectoryName(cab)!);
        File.WriteAllText(cab, "cab-bytes");
        metadata.Files.PartnerCab.Sha256 = ReleaseStaging.Sha256File(cab);
        ReleaseStaging.WriteMetadata(Path.Combine(download, "release-metadata", ReleaseStaging.MetadataFileName), metadata);
        Directory.CreateDirectory(Path.Combine(download, "control-app", "bin"));
        File.WriteAllText(Path.Combine(download, "control-app", "bin", "ControlApp.exe"), "app");

        ReleaseStaging.ArrangeDownloadedArtifacts(download, artifacts);
        AssertTrue(File.Exists(Path.Combine(artifacts, "bin", "ControlApp.exe")), "control app staged");
        AssertTrue(File.Exists(Path.Combine(artifacts, "submission", metadata.Files.PartnerCab.Name)), "cab staged");

        metadata.Files.PartnerCab.Sha256 = new string('0', 64);
        ReleaseStaging.WriteMetadata(Path.Combine(download, "release-metadata", ReleaseStaging.MetadataFileName), metadata);
        AssertThrows(() => ReleaseStaging.ArrangeDownloadedArtifacts(download, artifacts), "hash mismatch");
    }

    static void TestStageMicrosoftDrivers()
    {
        using TempScope scope = new();
        string download = Path.Combine(scope.Root, "download");
        string artifacts = Path.Combine(scope.Root, "artifacts");
        WriteDriverPackage(Path.Combine(download, "dshidmini-microsoft-drivers"));
        AssertTrue(ReleaseStaging.TryStageMicrosoftDrivers(download, artifacts), "drivers staged");
        AssertTrue(File.Exists(Path.Combine(artifacts, "drivers", "dshidmini.inf")), "inf staged");
        AssertTrue(File.Exists(Path.Combine(artifacts, "drivers", "x64", "dshidmini.dll")), "x64 staged");
        AssertTrue(!ReleaseStaging.TryStageMicrosoftDrivers(Path.Combine(scope.Root, "empty"), artifacts), "missing drivers");

        string badDownload = Path.Combine(scope.Root, "bad-download");
        Directory.CreateDirectory(Path.Combine(badDownload, "dshidmini-microsoft-drivers"));
        File.WriteAllText(Path.Combine(badDownload, "dshidmini-microsoft-drivers", "dshidmini.inf"), "inf");
        AssertThrows(() => ReleaseStaging.TryStageMicrosoftDrivers(badDownload, artifacts), "invalid source");
        AssertTrue(File.Exists(Path.Combine(artifacts, "drivers", "dshidmini.inf")), "previous drivers kept");
        AssertTrue(File.Exists(Path.Combine(artifacts, "drivers", "x64", "dshidmini.dll")), "previous x64 kept");
    }

    static void TestUniquePackageDetection()
    {
        using TempScope scope = new();
        AssertThrows(() => ReleaseStaging.FindUniqueDriverPackage(scope.Root), "no package");

        string first = Path.Combine(scope.Root, "a", "dshidmini");
        Directory.CreateDirectory(first);
        File.WriteAllText(Path.Combine(first, "dshidmini.inf"), "inf");
        AssertEqual(ReleaseStaging.FindUniqueDriverPackage(scope.Root), Path.GetFullPath(first), "one package");

        string second = Path.Combine(scope.Root, "b", "dshidmini");
        Directory.CreateDirectory(second);
        File.WriteAllText(Path.Combine(second, "dshidmini.inf"), "inf");
        AssertThrows(() => ReleaseStaging.FindUniqueDriverPackage(scope.Root), "two packages");
    }

    static void TestIngestFromDirectoryAndZip()
    {
        using TempScope scope = new();
        string package = WriteDriverPackage(Path.Combine(scope.Root, "extracted", "dshidmini"));
        string drivers = Path.Combine(scope.Root, "drivers");
        ReleaseStaging.IngestMicrosoftPackage(package, drivers);
        ReleaseStaging.RequireDriverLayout(drivers);

        string zip = Path.Combine(scope.Root, "signed.zip");
        ZipFile.CreateFromDirectory(Path.Combine(scope.Root, "extracted"), zip);
        string driversFromZip = Path.Combine(scope.Root, "drivers-zip");
        ReleaseStaging.IngestMicrosoftPackage(zip, driversFromZip);
        ReleaseStaging.RequireDriverLayout(driversFromZip);

        string missing = Path.Combine(scope.Root, "bad", "dshidmini");
        Directory.CreateDirectory(missing);
        File.WriteAllText(Path.Combine(missing, "dshidmini.inf"), "inf");
        AssertThrows(
            () => ReleaseStaging.IngestMicrosoftPackage(missing, Path.Combine(scope.Root, "drivers-bad")),
            "incomplete package");
    }

    static void TestIgfilterStaging()
    {
        using TempScope scope = new();
        string source = Path.Combine(scope.Root, "ig-src");
        WriteIgfilterPackage(Path.Combine(source, "nssmkig_x64"));
        WriteIgfilterPackage(Path.Combine(source, "nssmkig_arm64"));
        string dest = Path.Combine(scope.Root, "igfilter");
        ReleaseStaging.StageIgfilter(source, dest);
        ReleaseStaging.RequireIgfilterLayout(dest);
        AssertTrue(Directory.Exists(Path.Combine(dest, "nssmkig_ARM64")), "arm64 normalized");

        AssertThrows(() => ReleaseStaging.StageIgfilter(Path.Combine(scope.Root, "empty"), dest), "missing igfilter");
    }

    static void TestSetupValidationWithoutSignatures()
    {
        using TempScope scope = new();
        string artifacts = Path.Combine(scope.Root, "artifacts");
        ReleaseMetadata metadata = SampleMetadata();
        ReleaseStaging.WriteMetadata(ReleaseStaging.MetadataPath(artifacts), metadata);
        WriteDriverPackage(ReleaseStaging.DriversDirectory(artifacts), includeBinaries: false);
        Directory.CreateDirectory(ReleaseStaging.BinDirectory(artifacts));
        File.WriteAllText(Path.Combine(ReleaseStaging.BinDirectory(artifacts), "ControlApp.exe"), "app");
        WriteIgfilterPackage(Path.Combine(ReleaseStaging.IgfilterDirectory(artifacts), "nssmkig_x64"));
        WriteIgfilterPackage(Path.Combine(ReleaseStaging.IgfilterDirectory(artifacts), "nssmkig_ARM64"));

        AssertThrows(
            () => ReleaseStaging.ValidateSetupInputs(artifacts, "3.6.0", requireSignatures: false),
            "driver files without versions fail");
    }

    static void TestSignatureParser()
    {
        const string output = """
            Signing Certificate Chain:
                Issued to: DigiCert Assured ID Root CA
                Issued by: DigiCert Assured ID Root CA
                    Issued to: DigiCert Trusted G4 Code Signing RSA4096 SHA384 2021 CA1
                    Issued by: DigiCert Assured ID Root CA
                        Issued to: Nefarius Software Solutions e.U.
                        Issued by: DigiCert Trusted G4 Code Signing RSA4096 SHA384 2021 CA1
            The signature is timestamped: Thu Jan 01 00:00:00 2026
            Timestamp Verified by:
                Issued to: Timestamp Authority
                Issued by: Timestamp Root
            Signing Certificate Chain:
                Issued to: Microsoft Root Certificate Authority 2010
                Issued by: Microsoft Root Certificate Authority 2010
                    Issued to: Microsoft Windows Third Party Component CA 2014
                    Issued by: Microsoft Root Certificate Authority 2010
                        Issued to: Microsoft Windows Hardware Compatibility Publisher
                        Issued by: Microsoft Windows Third Party Component CA 2014
            """;
        var issued = ReleaseStaging.ParseIssuedTo(output);
        AssertEqual(string.Join("|", issued),
            "Nefarius Software Solutions e.U.|Microsoft Windows Hardware Compatibility Publisher",
            nameof(TestSignatureParser));
        ReleaseStaging.RequireDualDriverSigners(issued, "dshidmini.dll");
        ReleaseStaging.RequirePublisherSigner(issued, "ControlApp.exe");

        AssertThrows(
            () => ReleaseStaging.RequireDualDriverSigners(["Nefarius Software Solutions e.U."], "dshidmini.dll"),
            "publisher-only rejected");
        AssertThrows(
            () => ReleaseStaging.RequireDualDriverSigners(["Microsoft Windows Hardware Compatibility Publisher"], "dshidmini.dll"),
            "microsoft-only rejected");
    }

    static void TestSetupMsiContractAcceptsControlAppPayload()
    {
        IReadOnlyList<string> errors = SetupMsiContract.Validate(ValidSetupMsiContents());
        AssertTrue(errors.Count == 0, nameof(TestSetupMsiContractAcceptsControlAppPayload));
        AssertTrue(
            SetupMsiContract.ContainsMsiName(["CONTRO~1.EXE|ControlApp.exe"], SetupMsiContract.ControlAppFileName),
            "decodes MSI long file name");
        AssertEqual(
            SetupMsiContract.DecodeMsiName("DSHIDM~1|DsHidMini Control App"),
            SetupMsiContract.ControlAppShortcutName,
            "decodes MSI long shortcut name");
    }

    static void TestSetupMsiContractRejectsMissingControlApp()
    {
        SetupMsiContents contents = ValidSetupMsiContents() with { FileNames = ["dshidmini.dll"] };
        IReadOnlyList<string> errors = SetupMsiContract.Validate(contents);
        AssertTrue(errors.Any(error => error.Contains(SetupMsiContract.ControlAppFileName, StringComparison.Ordinal)),
            nameof(TestSetupMsiContractRejectsMissingControlApp));
    }

    static void TestSetupMsiContractRejectsMissingShortcut()
    {
        SetupMsiContents contents = ValidSetupMsiContents() with { ShortcutNames = ["Unrelated"] };
        IReadOnlyList<string> errors = SetupMsiContract.Validate(contents);
        AssertTrue(
            errors.Any(error => error.Contains(SetupMsiContract.ControlAppShortcutName, StringComparison.Ordinal)),
            nameof(TestSetupMsiContractRejectsMissingShortcut));
    }

    static void TestSetupMsiContractRejectsMissingDotNetPrerequisite()
    {
        SetupMsiContents missingAction = ValidSetupMsiContents() with { CustomActions = [] };
        AssertTrue(
            SetupMsiContract.Validate(missingAction)
                .Any(error => error.Contains(SetupMsiContract.DotNetRuntimeCustomAction, StringComparison.Ordinal)),
            "missing custom action");

        SetupMsiContents missingCondition = ValidSetupMsiContents() with
        {
            SequenceEntries =
            [
                new SetupMsiSequenceEntry
                {
                    Table = "InstallExecuteSequence",
                    Action = SetupMsiContract.DotNetRuntimeCustomAction,
                    Condition = "Installed"
                }
            ]
        };
        AssertTrue(
            SetupMsiContract.Validate(missingCondition)
                .Any(error => error.Contains(SetupMsiContract.NotInstalledCondition, StringComparison.Ordinal)),
            "missing NOT Installed condition");

        SetupMsiContents staleRuntimeError = ValidSetupMsiContents() with
        {
            Errors =
            [
                new SetupMsiError
                {
                    Id = SetupMsiContract.DotNetRuntimeErrorId,
                    Message = "The .NET 9 Desktop Runtime (x64) is required by DsHidMini Control App."
                }
            ]
        };
        AssertTrue(
            SetupMsiContract.Validate(staleRuntimeError)
                .Any(error => error.Contains(SetupMsiContract.DotNetRuntimeErrorHint, StringComparison.Ordinal)),
            "stale .NET 9 error text");
    }

    static void TestSetupMsiContractRejectsNearMatchAndUiOnlyRuntimeAction()
    {
        SetupMsiContents nearMatch = ValidSetupMsiContents() with
        {
            CustomActions =
            [
                new SetupMsiCustomAction
                {
                    Id = "CheckDotNetRuntimeProbe",
                    Source = "ActionRuntime.dll",
                    Target = SetupMsiContract.DotNetRuntimeCustomAction
                }
            ],
            SequenceEntries =
            [
                new SetupMsiSequenceEntry
                {
                    Table = "InstallExecuteSequence",
                    Action = "CheckDotNetRuntimeProbe",
                    Condition = SetupMsiContract.NotInstalledCondition
                }
            ]
        };
        AssertTrue(
            SetupMsiContract.Validate(nearMatch)
                .Any(error => error.Contains(SetupMsiContract.DotNetRuntimeCustomAction, StringComparison.Ordinal)),
            "near-match custom action");

        SetupMsiContents uiOnly = ValidSetupMsiContents() with
        {
            SequenceEntries =
            [
                new SetupMsiSequenceEntry
                {
                    Table = "InstallUISequence",
                    Action = SetupMsiContract.DotNetRuntimeCustomAction,
                    Condition = SetupMsiContract.NotInstalledCondition
                }
            ]
        };
        AssertTrue(
            SetupMsiContract.Validate(uiOnly)
                .Any(error => error.Contains("InstallExecuteSequence", StringComparison.Ordinal)),
            "UI-only sequence");
    }

    static SetupMsiContents ValidSetupMsiContents() => new()
    {
        FileNames = ["CONTRO~1.EXE|ControlApp.exe"],
        ShortcutNames = ["DSHIDM~1|DsHidMini Control App"],
        CustomActions =
        [
            new SetupMsiCustomAction
            {
                Id = SetupMsiContract.DotNetRuntimeCustomAction,
                Source = "ActionRuntime.dll",
                Target = SetupMsiContract.DotNetRuntimeCustomAction
            }
        ],
        SequenceEntries =
        [
            new SetupMsiSequenceEntry
            {
                Table = "InstallExecuteSequence",
                Action = SetupMsiContract.DotNetRuntimeCustomAction,
                Condition = SetupMsiContract.NotInstalledCondition
            }
        ],
        Errors =
        [
            new SetupMsiError
            {
                Id = SetupMsiContract.DotNetRuntimeErrorId,
                Message = "The .NET 10 Desktop Runtime (x64) is required by DsHidMini Control App."
            }
        ]
    };

    static ReleaseMetadata SampleMetadata() => new()
    {
        SchemaVersion = 1,
        Tag = "v3.6.0",
        SetupVersion = "3.6.0",
        DriverVersion = "3.6.0.2145",
        Commit = "abc",
        RunId = 123,
        Repository = "nefarius/DsHidMini",
        PublisherSubject = ReleaseStaging.PublisherSubject,
        Artifacts = new ReleaseArtifactNames
        {
            PartnerSubmission = "dshidmini-partner-submission",
            ControlApp = "control-app",
            Metadata = "release-metadata"
        },
        Files = new ReleaseFiles
        {
            PartnerCab = new ReleaseFile
            {
                Name = "dshidmini_3.6.0.2145.cab",
                Sha256 = new string('a', 64)
            }
        }
    };

    static string WriteDriverPackage(string directory, bool includeBinaries = true)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "dshidmini.inf"), "[Version]");
        File.WriteAllText(Path.Combine(directory, "dshidmini.cat"), "cat");
        Directory.CreateDirectory(Path.Combine(directory, "x64"));
        Directory.CreateDirectory(Path.Combine(directory, "ARM64"));
        if (includeBinaries)
        {
            File.WriteAllText(Path.Combine(directory, "x64", "dshidmini.dll"), "x64");
            File.WriteAllText(Path.Combine(directory, "ARM64", "dshidmini.dll"), "arm");
        }

        return directory;
    }

    static void WriteIgfilterPackage(string directory)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "igfilter.inf"), "[Version]");
        File.WriteAllText(Path.Combine(directory, "nssmkig.sys"), "sys");
    }

    static void AssertEqual(string actual, string expected, string name)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"FAIL {name}: expected '{expected}', got '{actual}'");
        }
    }

    static void AssertTrue(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"FAIL {name}");
        }
    }

    static void AssertThrows(Action action, string name)
    {
        try
        {
            action();
        }
        catch
        {
            return;
        }

        throw new InvalidOperationException($"FAIL {name}: expected an exception");
    }

    sealed class TempScope : IDisposable
    {
        public string Root { get; } = Directory.CreateTempSubdirectory("dshm-reltest-").FullName;

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
