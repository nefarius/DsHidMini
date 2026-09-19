#nullable enable
using System;
using System.IO;
using System.Reflection;

namespace Nefarius.DsHidMini.Setup;

/// <summary>
///     MakeSfxCA/WixSharp sometimes stores a support assembly under its simple name
///     (<c>CliWrap</c>) instead of <c>CliWrap.dll</c>. The CLR still probes the latter,
///     so deferred custom actions fail with FileNotFoundException unless this resolver
///     maps the requested name back to the extracted file.
/// </summary>
internal static class CustomActionAssemblyResolver
{
    internal static void Register()
    {
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
    }

    private static Assembly? Resolve(object? sender, ResolveEventArgs args)
    {
        try
        {
            AssemblyName requested = new(args.Name);
            if (string.IsNullOrEmpty(requested.Name))
            {
                return null;
            }

            string? directory = Path.GetDirectoryName(typeof(CustomActionAssemblyResolver).Assembly.Location);
            if (string.IsNullOrEmpty(directory))
            {
                return null;
            }

            string[] candidates =
            {
                Path.Combine(directory, requested.Name + ".dll"),
                Path.Combine(directory, requested.Name + ".exe"),
                Path.Combine(directory, requested.Name)
            };

            foreach (string candidate in candidates)
            {
                if (!File.Exists(candidate))
                {
                    continue;
                }

                AssemblyName actual = AssemblyName.GetAssemblyName(candidate);
                if (string.Equals(actual.Name, requested.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return Assembly.LoadFrom(candidate);
                }
            }
        }
        catch
        {
            // AssemblyResolve must never throw; the original bind failure is enough.
        }

        return null;
    }
}
