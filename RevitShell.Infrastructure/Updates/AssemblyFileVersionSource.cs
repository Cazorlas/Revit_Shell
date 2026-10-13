using System;
using System.Reflection;
using RevitShell.Application;

namespace RevitShell.Infrastructure;

/// <summary>Reads the installed assembly version without loading it into Explorer.</summary>
public sealed class AssemblyFileVersionSource : IInstalledVersionSource
{
    private readonly string _assemblyPath;

    /// <summary>Initializes the path to the installed shell assembly.</summary>
    public AssemblyFileVersionSource(string assemblyPath)
    {
        _assemblyPath = assemblyPath ?? throw new ArgumentNullException(nameof(assemblyPath));
    }

    /// <inheritdoc />
    public Version? GetInstalledVersion()
    {
        try
        {
            var version = AssemblyName.GetAssemblyName(_assemblyPath).Version;
            return version == null ? null : new Version(version.Major, version.Minor, Math.Max(version.Build, 0));
        }
        catch (Exception)
        {
            // Missing or unreadable files fall back to the running assembly version.
            return null;
        }
    }
}
