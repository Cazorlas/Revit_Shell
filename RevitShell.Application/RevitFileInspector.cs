using System;
using System.IO;
using System.Linq;
using RevitShell.Domain;

namespace RevitShell.Application;

/// <summary>
/// Inspects Revit files and returns a normalized inspection result.
/// </summary>
public sealed class RevitFileInspector : IRevitFileInspector
{
    private readonly IRevitVersionDetector _versionDetector;

    /// <summary>
    /// Initializes a new instance of the <see cref="RevitFileInspector"/> class.
    /// </summary>
    /// <param name="versionDetector">The version detector used during inspection.</param>
    public RevitFileInspector(IRevitVersionDetector versionDetector)
    {
        _versionDetector = versionDetector ?? throw new ArgumentNullException(nameof(versionDetector));
    }

    /// <inheritdoc />
    public bool IsSupportedFile(string path)
    {
        return RevitFileTypes.SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public RevitInfo Inspect(string path)
    {
        var exists = File.Exists(path);
        var isSupported = IsSupportedFile(path);
        var version = exists && isSupported ? _versionDetector.DetectVersion(path) : null;

        return new RevitInfo(path, exists, isSupported, version);
    }
}
