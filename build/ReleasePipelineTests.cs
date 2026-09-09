using System;
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
        TestUniquePackageDetection();
        TestIngestFromDirectoryAndZip();
        TestIgfilterStaging();
        TestSetupValidationWithoutSignatures();
        TestSignatureParser();
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
                Issued to: Nefarius Software Solutions e.U.
                Issued by: Intermediate CA
                    Issued to: Intermediate CA
                    Issued by: Root CA
            The signature is timestamped: Thu Jan 01 00:00:00 2026
            Timestamp Verified by:
                Issued to: Timestamp Authority
                Issued by: Timestamp Root
            Signing Certificate Chain:
                Issued to: Microsoft Windows Hardware Compatibility Publisher
                Issued by: Microsoft Windows Third Party Component CA 2014
                    Issued to: Microsoft Windows Third Party Component CA 2014
                    Issued by: Microsoft Root Certificate Authority 2010
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
