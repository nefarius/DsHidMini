using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

static class ReleaseStaging
{
    public const string PublisherSubject = "Nefarius Software Solutions e.U.";
    public const string MicrosoftSignerHint = "Microsoft";
    public const string MetadataFileName = "release-metadata.json";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string DriversDirectory(string artifactsRoot) => Path.Combine(artifactsRoot, "drivers");

    public static string BinDirectory(string artifactsRoot) => Path.Combine(artifactsRoot, "bin");

    public static string SubmissionDirectory(string artifactsRoot) => Path.Combine(artifactsRoot, "submission");

    public static string MetadataPath(string artifactsRoot) => Path.Combine(artifactsRoot, MetadataFileName);

    public static ReleaseMetadata ReadMetadata(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Release metadata not found: {path}");
        }

        string json = File.ReadAllText(path);
        ReleaseMetadata metadata = JsonSerializer.Deserialize<ReleaseMetadata>(json, JsonOptions)
                                   ?? throw new InvalidOperationException($"Release metadata is empty or invalid: {path}");
        metadata.ValidateIdentity();
        return metadata;
    }

    public static void WriteMetadata(string path, ReleaseMetadata metadata)
    {
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(metadata, JsonOptions), new UTF8Encoding(false));
    }

    public static string Sha256File(string path)
    {
        using FileStream stream = File.OpenRead(path);
        byte[] hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static Version ReadFileVersion(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"File not found: {path}");
        }

        FileVersionInfo info = FileVersionInfo.GetVersionInfo(path);
        if (string.IsNullOrWhiteSpace(info.FileVersion) || !Version.TryParse(info.FileVersion, out Version version))
        {
            throw new InvalidOperationException($"File version is missing or invalid on {path}");
        }

        return version;
    }

    public static void RequireFileVersion(string path, Version expected)
    {
        Version actual = ReadFileVersion(path);
        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"File version mismatch for {path}: expected {expected}, found {actual}.");
        }
    }

    public static string FindExistingFile(string root, params string[] relativeCandidates)
    {
        foreach (string relative in relativeCandidates)
        {
            string path = Path.Combine(root, relative);
            if (File.Exists(path))
            {
                return path;
            }
        }

        IEnumerable<string> matches = Directory.Exists(root)
            ? relativeCandidates.SelectMany(name =>
                Directory.GetFiles(root, Path.GetFileName(name), SearchOption.AllDirectories)
                    .Where(path => path.Replace('\\', '/').EndsWith(name.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase)))
            : [];

        return matches.FirstOrDefault();
    }

    public static void ArrangeDownloadedArtifacts(string downloadDir, string artifactsRoot)
    {
        if (!Directory.Exists(downloadDir))
        {
            throw new InvalidOperationException($"Download directory not found: {downloadDir}");
        }

        Directory.CreateDirectory(artifactsRoot);
        Directory.CreateDirectory(BinDirectory(artifactsRoot));
        Directory.CreateDirectory(SubmissionDirectory(artifactsRoot));

        string controlApp = FindExistingFile(downloadDir, Path.Combine("bin", "ControlApp.exe"), "ControlApp.exe")
                            ?? throw new InvalidOperationException(
                                "Downloaded artifacts are missing ControlApp.exe from the control-app artifact.");
        File.Copy(controlApp, Path.Combine(BinDirectory(artifactsRoot), "ControlApp.exe"), overwrite: true);

        string metadata = FindExistingFile(downloadDir, MetadataFileName)
                          ?? throw new InvalidOperationException(
                              "Downloaded artifacts are missing release-metadata.json. Use a tagged Build workflow run.");
        File.Copy(metadata, MetadataPath(artifactsRoot), overwrite: true);
        ReadMetadata(MetadataPath(artifactsRoot));

        string cab = Directory.GetFiles(downloadDir, "dshidmini_*.cab", SearchOption.AllDirectories).SingleOrDefault()
                     ?? throw new InvalidOperationException(
                         "Downloaded artifacts are missing the dshidmini-partner-submission CAB.");
        string cabDest = Path.Combine(SubmissionDirectory(artifactsRoot), Path.GetFileName(cab));
        File.Copy(cab, cabDest, overwrite: true);

        ReleaseMetadata parsed = ReadMetadata(MetadataPath(artifactsRoot));
        if (parsed.Files?.PartnerCab is not { } partnerCab)
        {
            throw new InvalidOperationException("Release metadata is missing files.partnerCab.");
        }

        string actualHash = Sha256File(cabDest);
        if (!string.Equals(actualHash, partnerCab.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Partner CAB hash mismatch. Metadata has {partnerCab.Sha256}, file is {actualHash}.");
        }

        if (!string.Equals(Path.GetFileName(cabDest), partnerCab.Name, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Partner CAB name mismatch. Metadata has {partnerCab.Name}, file is {Path.GetFileName(cabDest)}.");
        }
    }

    public static bool TryStageMicrosoftDrivers(string downloadDir, string artifactsRoot)
    {
        IReadOnlyList<string> packages = FindStagedMicrosoftDriverPackages(downloadDir);
        if (packages.Count == 0)
        {
            return false;
        }

        if (packages.Count > 1)
        {
            throw new InvalidOperationException(
                "Multiple dshidmini driver packages were found; refusing to guess. Packages:" +
                Environment.NewLine + string.Join(Environment.NewLine, packages));
        }

        string destination = DriversDirectory(artifactsRoot);
        RequireDriverLayout(packages[0]);

        if (Directory.Exists(destination))
        {
            Directory.Delete(destination, recursive: true);
        }

        CopyDirectory(packages[0], destination);
        RequireDriverLayout(destination);
        return true;
    }

    static IReadOnlyList<string> FindStagedMicrosoftDriverPackages(string root)
    {
        if (!Directory.Exists(root))
        {
            return [];
        }

        string preferred = Path.Combine(root, "dshidmini-microsoft-drivers");
        if (File.Exists(Path.Combine(preferred, "dshidmini.inf")))
        {
            return [Path.GetFullPath(preferred)];
        }

        IReadOnlyList<string> named = FindDriverPackages(root);
        if (named.Count > 0)
        {
            return named;
        }

        return Directory.GetFiles(root, "dshidmini.inf", SearchOption.AllDirectories)
            .Select(Path.GetDirectoryName)
            .Where(directory => !string.IsNullOrWhiteSpace(directory))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<string> FindDriverPackages(string root)
    {
        if (!Directory.Exists(root))
        {
            return [];
        }

        if (File.Exists(Path.Combine(root, "dshidmini.inf")))
        {
            return [Path.GetFullPath(root)];
        }

        return Directory.GetDirectories(root, "dshidmini", SearchOption.AllDirectories)
            .Where(directory => File.Exists(Path.Combine(directory, "dshidmini.inf")))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string FindUniqueDriverPackage(string root)
    {
        IReadOnlyList<string> packages = FindDriverPackages(root);
        if (packages.Count == 0)
        {
            throw new InvalidOperationException(
                $"No dshidmini driver package (folder containing dshidmini.inf) was found under {root}.");
        }

        if (packages.Count > 1)
        {
            throw new InvalidOperationException(
                "Multiple dshidmini driver packages were found; refusing to guess. Packages:" +
                Environment.NewLine + string.Join(Environment.NewLine, packages));
        }

        return packages[0];
    }

    public static void ExtractArchive(string archivePath, string destination)
    {
        Directory.CreateDirectory(destination);
        string extension = Path.GetExtension(archivePath);
        if (extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            ZipFile.ExtractToDirectory(archivePath, destination, overwriteFiles: true);
            return;
        }

        if (extension.Equals(".cab", StringComparison.OrdinalIgnoreCase))
        {
            ProcessStartInfo info = new()
            {
                FileName = "expand.exe",
                Arguments = $"-F:* \"{archivePath}\" \"{destination}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using Process process = Process.Start(info)
                                    ?? throw new InvalidOperationException("Failed to start expand.exe.");
            Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
            Task<string> standardError = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            _ = standardOutput.GetAwaiter().GetResult();
            string error = standardError.GetAwaiter().GetResult();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"expand.exe failed ({process.ExitCode}) for {archivePath}: {error}");
            }

            return;
        }

        throw new InvalidOperationException($"Unsupported Microsoft package archive: {archivePath}");
    }

    public static void IngestMicrosoftPackage(string source, string driversDir)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new InvalidOperationException("MicrosoftPackagePath is required.");
        }

        string resolved = Path.GetFullPath(source);
        string packageRoot;
        string temp = null;

        try
        {
            if (File.Exists(resolved))
            {
                temp = Directory.CreateTempSubdirectory("dshm-msft-").FullName;
                ExtractArchive(resolved, temp);
                packageRoot = FindUniqueDriverPackage(temp);
            }
            else if (Directory.Exists(resolved))
            {
                packageRoot = FindUniqueDriverPackage(resolved);
            }
            else
            {
                throw new InvalidOperationException($"Microsoft package not found: {resolved}");
            }

            RequireDriverLayout(packageRoot);

            if (Directory.Exists(driversDir))
            {
                Directory.Delete(driversDir, recursive: true);
            }

            CopyDirectory(packageRoot, driversDir);
            RequireDriverLayout(driversDir);
        }
        finally
        {
            if (temp != null && Directory.Exists(temp))
            {
                Directory.Delete(temp, recursive: true);
            }
        }
    }

    public static void RequireDriverLayout(string driversDir)
    {
        string[] required =
        [
            Path.Combine(driversDir, "dshidmini.inf"),
            Path.Combine(driversDir, "dshidmini.cat"),
            Path.Combine(driversDir, "x64", "dshidmini.dll"),
            Path.Combine(driversDir, "ARM64", "dshidmini.dll")
        ];

        string arm64Alias = Path.Combine(driversDir, "arm64", "dshidmini.dll");
        if (!File.Exists(required[3]) && File.Exists(arm64Alias))
        {
            string destDir = Path.Combine(driversDir, "ARM64");
            Directory.CreateDirectory(destDir);
            File.Copy(arm64Alias, Path.Combine(destDir, "dshidmini.dll"), overwrite: true);
        }

        List<string> missing = required.Where(path => !File.Exists(path)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Driver package is missing required files:" + Environment.NewLine +
                string.Join(Environment.NewLine, missing) + Environment.NewLine +
                "Expected artifacts/drivers/{dshidmini.inf,dshidmini.cat,x64/dshidmini.dll,ARM64/dshidmini.dll}.");
        }
    }

    public static IReadOnlyList<string> ParseIssuedTo(string signToolOutput)
    {
        if (string.IsNullOrWhiteSpace(signToolOutput))
        {
            return [];
        }

        List<string> subjects = [];
        string[] sections = Regex.Split(signToolOutput, @"Signing Certificate Chain:\s*", RegexOptions.IgnoreCase);
        for (int i = 1; i < sections.Length; i++)
        {
            string section = sections[i];
            int timestampIndex = section.IndexOf("Timestamp Verified by:", StringComparison.OrdinalIgnoreCase);
            if (timestampIndex >= 0)
            {
                section = section[..timestampIndex];
            }

            // signtool /v prints each chain root-first. The leaf (the actual signer)
            // is the last Issued to in the section.
            MatchCollection matches = Regex.Matches(section, @"Issued to:\s*(.+)");
            if (matches.Count == 0)
            {
                continue;
            }

            string value = matches[^1].Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                subjects.Add(value);
            }
        }

        return subjects;
    }

    public static void RequireDualDriverSigners(IReadOnlyList<string> issuedTo, string file)
    {
        bool publisher = issuedTo.Any(value =>
            value.Contains(PublisherSubject, StringComparison.OrdinalIgnoreCase));
        bool microsoft = issuedTo.Any(value =>
            value.Contains(MicrosoftSignerHint, StringComparison.OrdinalIgnoreCase) &&
            !value.Contains(PublisherSubject, StringComparison.OrdinalIgnoreCase));

        if (!publisher || !microsoft)
        {
            throw new InvalidOperationException(
                $"{file} must contain both the publisher signature ({PublisherSubject}) and the Microsoft attestation signature. Signers found: {string.Join("; ", issuedTo)}");
        }
    }

    static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, destination, StringComparison.OrdinalIgnoreCase));
        }

        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string dest = file.Replace(source, destination, StringComparison.OrdinalIgnoreCase);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }
}

sealed class ReleaseMetadata
{
    public int SchemaVersion { get; set; }

    public string Tag { get; set; }

    public string SetupVersion { get; set; }

    public string DriverVersion { get; set; }

    public string Commit { get; set; }

    public long RunId { get; set; }

    public string Repository { get; set; }

    public string PublisherSubject { get; set; }

    public ReleaseArtifactNames Artifacts { get; set; }

    public ReleaseFiles Files { get; set; }

    public void ValidateIdentity()
    {
        if (string.IsNullOrWhiteSpace(Tag) || !Regex.IsMatch(Tag, @"^v\d+\.\d+\.\d+$"))
        {
            throw new InvalidOperationException($"Release metadata tag is missing or not vMAJOR.MINOR.PATCH: '{Tag}'.");
        }

        if (string.IsNullOrWhiteSpace(SetupVersion) || SetupVersion != Tag[1..])
        {
            throw new InvalidOperationException(
                $"Release metadata setupVersion '{SetupVersion}' must match tag '{Tag}' without the v prefix.");
        }

        if (string.IsNullOrWhiteSpace(DriverVersion) ||
            DriverVersion.Split('.').Length != 4 ||
            !Version.TryParse(DriverVersion, out _))
        {
            throw new InvalidOperationException($"Release metadata driverVersion is invalid: '{DriverVersion}'.");
        }

        if (!DriverVersion.StartsWith(SetupVersion + ".", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Release metadata driverVersion '{DriverVersion}' is not derived from setupVersion '{SetupVersion}'.");
        }

        if (RunId <= 0)
        {
            throw new InvalidOperationException("Release metadata runId is missing.");
        }

        if (Files?.PartnerCab is not { } partnerCab)
        {
            throw new InvalidOperationException("Release metadata is missing files.partnerCab.");
        }

        if (string.IsNullOrWhiteSpace(partnerCab.Name) || string.IsNullOrWhiteSpace(partnerCab.Sha256))
        {
            throw new InvalidOperationException("Release metadata files.partnerCab must include name and sha256.");
        }
    }
}

sealed class ReleaseArtifactNames
{
    public string PartnerSubmission { get; set; }

    public string ControlApp { get; set; }

    public List<string> Platforms { get; set; }

    public string Metadata { get; set; }
}

sealed class ReleaseFiles
{
    public ReleaseFile PartnerCab { get; set; }
}

sealed class ReleaseFile
{
    public string Name { get; set; }

    public string Sha256 { get; set; }
}

