using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
static class SetupMsiContract
{
    public const string ControlAppFileName = "ControlApp.exe";
    public const string ControlAppShortcutName = "DsHidMini Control App";
    public const string DotNetRuntimeCustomAction = "CheckDotNetRuntime";
    public const string DotNetRuntimeErrorId = "9001";
    public const string DotNetRuntimeErrorHint = ".NET 10 Desktop Runtime";
    public const string NotInstalledCondition = "NOT Installed";

    public static void ValidateGeneratedMsi(string msiPath)
    {
        IReadOnlyList<string> errors = Validate(ReadMsi(msiPath));
        if (errors.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "Generated MSI is missing the ControlApp packaging contract:" + Environment.NewLine +
            string.Join(Environment.NewLine, errors.Select(error => "- " + error)));
    }

    public static IReadOnlyList<string> Validate(SetupMsiContents contents)
    {
        ArgumentNullException.ThrowIfNull(contents);

        List<string> errors = [];

        if (!ContainsMsiName(contents.FileNames, ControlAppFileName))
        {
            errors.Add($"File table is missing {ControlAppFileName}.");
        }

        if (!ContainsMsiName(contents.ShortcutNames, ControlAppShortcutName))
        {
            errors.Add($"Shortcut table is missing '{ControlAppShortcutName}'.");
        }

        IReadOnlyList<SetupMsiCustomAction> runtimeActions = contents.CustomActions
            .Where(action =>
                ContainsIgnoreCase(action.Id, DotNetRuntimeCustomAction) ||
                ContainsIgnoreCase(action.Source, DotNetRuntimeCustomAction) ||
                ContainsIgnoreCase(action.Target, DotNetRuntimeCustomAction))
            .ToArray();

        if (runtimeActions.Count == 0)
        {
            errors.Add($"CustomAction table is missing {DotNetRuntimeCustomAction}.");
        }

        HashSet<string> runtimeActionIds = new(
            runtimeActions.Select(action => action.Id).Where(id => !string.IsNullOrWhiteSpace(id)),
            StringComparer.OrdinalIgnoreCase);
        if (runtimeActionIds.Count == 0)
        {
            runtimeActionIds.Add(DotNetRuntimeCustomAction);
        }

        bool sequenced = contents.SequenceEntries.Any(entry =>
            runtimeActionIds.Contains(entry.Action) &&
            ContainsIgnoreCase(entry.Condition, NotInstalledCondition));

        if (!sequenced)
        {
            errors.Add(
                $"{DotNetRuntimeCustomAction} is missing from InstallExecuteSequence/InstallUISequence " +
                $"with condition '{NotInstalledCondition}'.");
        }

        bool runtimeError = contents.Errors.Any(error =>
            string.Equals(error.Id, DotNetRuntimeErrorId, StringComparison.OrdinalIgnoreCase) &&
            ContainsIgnoreCase(error.Message, DotNetRuntimeErrorHint));

        if (!runtimeError)
        {
            errors.Add(
                $"Error {DotNetRuntimeErrorId} must mention '{DotNetRuntimeErrorHint}'.");
        }

        return errors;
    }

    public static SetupMsiContents ReadMsi(string msiPath)
    {
        if (!File.Exists(msiPath))
        {
            throw new InvalidOperationException($"MSI not found: {msiPath}");
        }

        Type installerType = Type.GetTypeFromProgID("WindowsInstaller.Installer")
                             ?? throw new InvalidOperationException(
                                 "Windows Installer COM (WindowsInstaller.Installer) is unavailable.");

        object installer = Activator.CreateInstance(installerType)
                           ?? throw new InvalidOperationException("Failed to create Windows Installer COM object.");
        object database = null;
        try
        {
            database = installerType.InvokeMember(
                "OpenDatabase",
                System.Reflection.BindingFlags.InvokeMethod,
                binder: null,
                target: installer,
                args: [msiPath, 0]);

            HashSet<string> tables = new(ReadColumn(database, "SELECT `Name` FROM `_Tables`"), StringComparer.OrdinalIgnoreCase);
            return new SetupMsiContents
            {
                FileNames = tables.Contains("File")
                    ? ReadColumn(database, "SELECT `FileName` FROM `File`")
                    : [],
                ShortcutNames = tables.Contains("Shortcut")
                    ? ReadColumn(database, "SELECT `Name` FROM `Shortcut`")
                    : [],
                CustomActions = tables.Contains("CustomAction")
                    ? ReadCustomActions(database)
                    : [],
                SequenceEntries = ReadSequenceEntries(database, tables),
                Errors = tables.Contains("Error")
                    ? ReadErrors(database)
                    : []
            };
        }
        finally
        {
            ReleaseCom(database);
            ReleaseCom(installer);
        }
    }

    public static bool ContainsMsiName(IEnumerable<string> values, string expected)
    {
        return values.Any(value =>
            string.Equals(DecodeMsiName(value), expected, StringComparison.OrdinalIgnoreCase));
    }

    public static string DecodeMsiName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        int pipe = value.IndexOf('|');
        return pipe >= 0 ? value[(pipe + 1)..] : value;
    }

    static IReadOnlyList<SetupMsiCustomAction> ReadCustomActions(object database)
    {
        return ReadRows(database, "SELECT `Action`, `Source`, `Target` FROM `CustomAction`")
            .Select(row => new SetupMsiCustomAction
            {
                Id = row.ElementAtOrDefault(0),
                Source = row.ElementAtOrDefault(1),
                Target = row.ElementAtOrDefault(2)
            })
            .ToArray();
    }

    static IReadOnlyList<SetupMsiSequenceEntry> ReadSequenceEntries(object database, ISet<string> tables)
    {
        List<SetupMsiSequenceEntry> entries = [];
        foreach (string table in new[] { "InstallExecuteSequence", "InstallUISequence" })
        {
            if (!tables.Contains(table))
            {
                continue;
            }

            entries.AddRange(
                ReadRows(database, $"SELECT `Action`, `Condition` FROM `{table}`")
                    .Select(row => new SetupMsiSequenceEntry
                    {
                        Table = table,
                        Action = row.ElementAtOrDefault(0),
                        Condition = row.ElementAtOrDefault(1)
                    }));
        }

        return entries;
    }

    static IReadOnlyList<SetupMsiError> ReadErrors(object database)
    {
        return ReadRows(database, "SELECT `Error`, `Message` FROM `Error`")
            .Select(row => new SetupMsiError
            {
                Id = row.ElementAtOrDefault(0),
                Message = row.ElementAtOrDefault(1)
            })
            .ToArray();
    }

    static IReadOnlyList<string> ReadColumn(object database, string sql)
    {
        return ReadRows(database, sql)
            .Select(row => row.ElementAtOrDefault(0))
            .Where(value => !string.IsNullOrEmpty(value))
            .ToArray();
    }

    static IReadOnlyList<string[]> ReadRows(object database, string sql)
    {
        object view = database.GetType().InvokeMember(
            "OpenView",
            System.Reflection.BindingFlags.InvokeMethod,
            binder: null,
            target: database,
            args: [sql]);

        try
        {
            view.GetType().InvokeMember(
                "Execute",
                System.Reflection.BindingFlags.InvokeMethod,
                binder: null,
                target: view,
                args: [null]);

            List<string[]> rows = [];
            while (true)
            {
                object record = view.GetType().InvokeMember(
                    "Fetch",
                    System.Reflection.BindingFlags.InvokeMethod,
                    binder: null,
                    target: view,
                    args: null);
                if (record == null)
                {
                    break;
                }

                try
                {
                    int fieldCount = Convert.ToInt32(
                        record.GetType().InvokeMember(
                            "FieldCount",
                            System.Reflection.BindingFlags.GetProperty,
                            binder: null,
                            target: record,
                            args: null));
                    string[] row = new string[fieldCount];
                    for (int i = 1; i <= fieldCount; i++)
                    {
                        row[i - 1] = Convert.ToString(
                            record.GetType().InvokeMember(
                                "StringData",
                                System.Reflection.BindingFlags.GetProperty,
                                binder: null,
                                target: record,
                                args: [i]));
                    }

                    rows.Add(row);
                }
                finally
                {
                    ReleaseCom(record);
                }
            }

            return rows;
        }
        finally
        {
            ReleaseCom(view);
        }
    }

    static bool ContainsIgnoreCase(string value, string expected)
    {
        return !string.IsNullOrEmpty(value) &&
               value.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static void ReleaseCom(object value)
    {
        if (value != null && Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }
}

sealed record SetupMsiContents
{
    public IReadOnlyList<string> FileNames { get; init; } = [];

    public IReadOnlyList<string> ShortcutNames { get; init; } = [];

    public IReadOnlyList<SetupMsiCustomAction> CustomActions { get; init; } = [];

    public IReadOnlyList<SetupMsiSequenceEntry> SequenceEntries { get; init; } = [];

    public IReadOnlyList<SetupMsiError> Errors { get; init; } = [];
}

sealed class SetupMsiCustomAction
{
    public string Id { get; init; }

    public string Source { get; init; }

    public string Target { get; init; }
}

sealed class SetupMsiSequenceEntry
{
    public string Table { get; init; }

    public string Action { get; init; }

    public string Condition { get; init; }
}

sealed class SetupMsiError
{
    public string Id { get; init; }

    public string Message { get; init; }
}
