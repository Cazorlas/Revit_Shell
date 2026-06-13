using System.IO;
using System.Runtime.InteropServices;

namespace RevitShell.Core;

internal static class StructuredStorageUtils
{
    [DllImport("ole32.dll")]
    private static extern int StgIsStorageFile([MarshalAs(UnmanagedType.LPWStr)] string path);

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
