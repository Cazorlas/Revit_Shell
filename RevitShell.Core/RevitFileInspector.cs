using System;
using System.IO;
using System.Linq;

namespace RevitShell.Core;

public sealed class RevitFileInspector : IRevitFileInspector
{
    private readonly IRevitVersionDetector _versionDetector;

    public RevitFileInspector(IRevitVersionDetector versionDetector)
    {
        _versionDetector = versionDetector ?? throw new ArgumentNullException(nameof(versionDetector));
    }

    public bool IsSupportedFile(string path)
    {
        return RevitFileTypes.SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }

    public RevitFileInspectionResult Inspect(string path)
    {
        var exists = File.Exists(path);
        var isSupported = IsSupportedFile(path);
        var version = exists && isSupported ? _versionDetector.DetectVersion(path) : null;

        return new RevitFileInspectionResult(path, exists, isSupported, version);
    }
}
