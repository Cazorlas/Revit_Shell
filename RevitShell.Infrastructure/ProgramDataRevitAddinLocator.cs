using System;
using System.Globalization;
using System.IO;
using RevitShell.Application;

namespace RevitShell.Infrastructure;

/// <summary>Checks the per-year ProgramData opening helper manifest.</summary>
public sealed class ProgramDataRevitAddinLocator : IRevitAddinLocator
{
    private readonly string _rootDirectory;

    /// <summary>Uses the standard Revit ProgramData add-ins directory.</summary>
    public ProgramDataRevitAddinLocator()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Autodesk", "Revit", "Addins"))
    {
    }

    /// <summary>Uses a supplied add-ins root directory.</summary>
    /// <param name="rootDirectory">The directory containing per-year folders.</param>
    public ProgramDataRevitAddinLocator(string rootDirectory)
    {
        _rootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
    }

    /// <inheritdoc />
    public bool IsInstalled(int revitVersion) => File.Exists(Path.Combine(_rootDirectory,
        revitVersion.ToString(CultureInfo.InvariantCulture), "RevitShell.OpenHelper.addin"));
}
