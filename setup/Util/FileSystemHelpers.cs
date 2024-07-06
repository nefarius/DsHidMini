using System.IO;

namespace Nefarius.DsHidMini.Setup.Util;

public static class FileSystemHelpers
{
    public static string GetTemporaryDirectory()
    {
        string tempPath = Path.GetTempPath();
        string tempDirectory = Path.Combine(tempPath, Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);
        return tempDirectory;
    }
}