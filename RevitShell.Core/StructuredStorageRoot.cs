using System;
using System.IO;
using System.IO.Packaging;
using System.Reflection;

namespace RevitShell.Core;

internal sealed class StructuredStorageRoot : IDisposable
{
    private readonly StorageInfo _storageRoot;

    public StructuredStorageRoot(string fileName)
    {
        _storageRoot = (StorageInfo)InvokeStorageRootMethod(
            null,
            "Open",
            fileName,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
    }

    public StorageInfo BaseRoot => _storageRoot;

    public void Dispose()
    {
        InvokeStorageRootMethod(_storageRoot, "Close");
    }

    private static object InvokeStorageRootMethod(StorageInfo storageRoot, string methodName, params object[] methodArgs)
    {
        var storageRootType = typeof(StorageInfo).Assembly.GetType("System.IO.Packaging.StorageRoot", true, false);

        return storageRootType.InvokeMember(
            methodName,
            BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
            null,
            storageRoot,
            methodArgs);
    }
}
