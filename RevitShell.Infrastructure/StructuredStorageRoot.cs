using System;
using System.IO;
using System.IO.Packaging;
using System.Reflection;

namespace RevitShell.Infrastructure;

/// <summary>
/// Wraps access to a structured storage root using the packaging API.
/// </summary>
internal sealed class StructuredStorageRoot : IDisposable
{
    private readonly StorageInfo _storageRoot;

    /// <summary>
    /// Initializes a new instance of the <see cref="StructuredStorageRoot"/> class.
    /// </summary>
    /// <param name="fileName">The file path to open as a structured storage container.</param>
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

    /// <summary>
    /// Gets the underlying storage root.
    /// </summary>
    public StorageInfo BaseRoot => _storageRoot;

    /// <summary>
    /// Releases the underlying storage root.
    /// </summary>
    public void Dispose()
    {
        InvokeStorageRootMethod(_storageRoot, "Close");
    }

    /// <summary>
    /// Invokes a non-public packaging storage root method by reflection.
    /// </summary>
    /// <param name="storageRoot">The storage root instance for instance calls, or <see langword="null"/> for static calls.</param>
    /// <param name="methodName">The method name to invoke.</param>
    /// <param name="methodArgs">The arguments to pass to the method.</param>
    /// <returns>The method return value.</returns>
    private static object InvokeStorageRootMethod(StorageInfo? storageRoot, string methodName, params object[] methodArgs)
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
