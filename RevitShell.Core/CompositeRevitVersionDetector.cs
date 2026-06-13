using System;

namespace RevitShell.Core;

public sealed class CompositeRevitVersionDetector : IRevitVersionDetector
{
    private readonly IRevitVersionDetector[] _detectors;

    public CompositeRevitVersionDetector(params IRevitVersionDetector[] detectors)
    {
        _detectors = detectors ?? throw new ArgumentNullException(nameof(detectors));
    }

    public int? DetectVersion(string path)
    {
        foreach (var detector in _detectors)
        {
            var version = detector?.DetectVersion(path);
            if (version.HasValue)
            {
                return version;
            }
        }

        return null;
    }
}
