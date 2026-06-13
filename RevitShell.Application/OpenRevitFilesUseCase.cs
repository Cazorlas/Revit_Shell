using System;
using System.IO;
using System.Linq;

namespace RevitShell.Application;

/// <summary>
/// Coordinates opening Revit files with their exact installed Revit versions.
/// </summary>
public sealed class OpenRevitFilesUseCase
{
    private readonly IRevitFileInspector _inspector;
    private readonly IRevitInstallationLocator _installationLocator;
    private readonly IRevitApplicationLauncher _applicationLauncher;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenRevitFilesUseCase"/> class.
    /// </summary>
    /// <param name="inspector">The inspector used to validate files and detect versions.</param>
    /// <param name="installationLocator">The locator used to resolve installed Revit applications.</param>
    /// <param name="applicationLauncher">The launcher used to start Revit with the selected file.</param>
    public OpenRevitFilesUseCase(
        IRevitFileInspector inspector,
        IRevitInstallationLocator installationLocator,
        IRevitApplicationLauncher applicationLauncher)
    {
        _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
        _installationLocator = installationLocator ?? throw new ArgumentNullException(nameof(installationLocator));
        _applicationLauncher = applicationLauncher ?? throw new ArgumentNullException(nameof(applicationLauncher));
    }

    /// <summary>
    /// Opens the supplied Revit files with their exact installed Revit versions.
    /// </summary>
    /// <param name="filePaths">The file paths selected by the user.</param>
    public void Execute(string[] filePaths)
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
            var revitInfo = _inspector.Inspect(filePath);
            if (!revitInfo.Exists)
            {
                throw new FileNotFoundException("Revit file not found.", filePath);
            }

            if (!revitInfo.IsSupported)
            {
                throw new InvalidOperationException($"Unsupported Revit file: {Path.GetFileName(filePath)}");
            }

            if (!revitInfo.HasDetectedVersion)
            {
                throw new InvalidOperationException($"Could not detect the Revit version for '{Path.GetFileName(filePath)}'.");
            }

            var version = revitInfo.Version!.Value;
            var installation = _installationLocator.FindExactMatch(version);
            if (installation == null)
            {
                throw new InvalidOperationException($"Revit {version} is not installed on this machine.");
            }

            _applicationLauncher.Launch(installation, filePath);
        }
    }
}
