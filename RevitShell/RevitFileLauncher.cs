using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RevitShell.Core;

namespace RevitShell;

internal sealed class RevitFileLauncher
{
    private readonly IRevitFileInspector _inspector;
    private readonly RegistryRevitApplicationLocator _applicationLocator;

    public RevitFileLauncher(IRevitFileInspector inspector, RegistryRevitApplicationLocator applicationLocator)
    {
        _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
        _applicationLocator = applicationLocator ?? throw new ArgumentNullException(nameof(applicationLocator));
    }

    public void Open(string[] filePaths)
    {
        var targets = filePaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim('"'))
            .ToArray();

        if (targets.Length == 0)
        {
            throw new InvalidOperationException("No Revit file was provided.");
        }

        foreach (var filePath in targets)
        {
            var result = _inspector.Inspect(filePath);
            if (!result.Exists)
            {
                throw new FileNotFoundException("Revit file not found.", filePath);
            }

            if (!result.IsSupported)
            {
                throw new InvalidOperationException($"Unsupported Revit file: {Path.GetFileName(filePath)}");
            }

            if (!result.Version.HasValue)
            {
                throw new InvalidOperationException($"Could not detect the Revit version for '{Path.GetFileName(filePath)}'.");
            }

            var installation = _applicationLocator.FindExactMatch(result.Version.Value);
            if (installation == null)
            {
                throw new InvalidOperationException($"Revit {result.Version.Value} is not installed on this machine.");
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = installation.ExecutablePath,
                Arguments = $"\"{filePath}\"",
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(installation.ExecutablePath) ?? string.Empty
            });
        }
    }
}
