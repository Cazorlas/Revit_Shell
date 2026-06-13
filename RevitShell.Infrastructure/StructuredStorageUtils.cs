using System.IO;
using System.Runtime.InteropServices;

namespace RevitShell.Infrastructure;

/// <summary>
/// Provides helper methods for working with compound structured storage files.
/// </summary>
internal static class StructuredStorageUtils
{
    [DllImport("ole32.dll")]
    private static extern int StgIsStorageFile([MarshalAs(UnmanagedType.LPWStr)] string path);

    /// <summary>
    /// Determines whether the specified file is a structured storage file.
    /// </summary>
    /// <param name="path">The file path to validate.</param>
    /// <param name="throwIfNotExist">
    /// <see langword="true"/> to throw when the file cannot be found; otherwise, <see langword="false"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the file is a structured storage file; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsFileStructuredStorage(string path, bool throwIfNotExist = true)
    {
        var result = StgIsStorageFile(path);

        if (result == 0)
        {
            return true;
        }

        if (result == 1)
        {
            return false;
        }

        if (throwIfNotExist)
        {
            throw new FileNotFoundException("File not found", path);
        }

        return false;
    }
}
