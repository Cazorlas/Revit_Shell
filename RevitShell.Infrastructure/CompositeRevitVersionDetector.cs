using System;
using RevitShell.Application;

namespace RevitShell.Infrastructure;

/// <summary>
/// Executes multiple version detectors in sequence until one returns a value.
/// </summary>
public sealed class CompositeRevitVersionDetector : IRevitVersionDetector
{
    private readonly IRevitVersionDetector[] _detectors;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeRevitVersionDetector"/> class.
    /// </summary>
    /// <param name="detectors">The detectors to evaluate in order.</param>
    public CompositeRevitVersionDetector(params IRevitVersionDetector[] detectors)
    {
        _detectors = detectors ?? throw new ArgumentNullException(nameof(detectors));
    }

    /// <inheritdoc />
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
