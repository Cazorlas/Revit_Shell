using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RevitShell.Domain;

namespace RevitShell.Application;

/// <summary>
/// Coordinates opening Revit files with their exact installed Revit versions.
/// </summary>
public sealed class OpenRevitFilesUseCase
{
    private readonly IRevitFileInspector _inspector;
    private readonly IRevitInstallationLocator _installationLocator;
    private readonly IRevitApplicationLauncher _applicationLauncher;
    private readonly IWorksharedOpenPrompt _prompt;
    private readonly IRevitAddinLocator _addinLocator;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenRevitFilesUseCase"/> class.
    /// </summary>
    /// <param name="inspector">The inspector used to validate files and detect versions.</param>
    /// <param name="installationLocator">The locator used to resolve installed Revit applications.</param>
    /// <param name="applicationLauncher">The launcher used to start Revit with the selected file.</param>
    /// <param name="prompt">The prompt for workshared opening choices.</param>
    /// <param name="addinLocator">The locator for the opening helper.</param>
    public OpenRevitFilesUseCase(
        IRevitFileInspector inspector,
        IRevitInstallationLocator installationLocator,
        IRevitApplicationLauncher applicationLauncher,
        IWorksharedOpenPrompt prompt,
        IRevitAddinLocator addinLocator)
    {
        _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
        _installationLocator = installationLocator ?? throw new ArgumentNullException(nameof(installationLocator));
        _applicationLauncher = applicationLauncher ?? throw new ArgumentNullException(nameof(applicationLauncher));
        _prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
        _addinLocator = addinLocator ?? throw new ArgumentNullException(nameof(addinLocator));
    }

    /// <summary>
    /// Opens the supplied Revit files with their exact installed Revit versions.
    /// </summary>
    /// <param name="filePaths">The file paths selected by the user.</param>
    /// <returns>The failures collected for individual files.</returns>
    public OpenRevitFilesResult Execute(string[] filePaths)
    {
        if (filePaths == null)
        {
            throw new ArgumentNullException(nameof(filePaths));
        }

        var targets = filePaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim('"'))
            .ToArray();

        if (targets.Length == 0)
        {
            throw new InvalidOperationException("No Revit file was provided.");
        }

        var failures = new List<OpenRevitFileFailure>();
        foreach (var filePath in targets)
        {
            try
            {
                OpenFile(filePath);
            }
            catch (Exception ex)
            {
                failures.Add(new OpenRevitFileFailure(filePath, ex.Message));
            }
        }

        return new OpenRevitFilesResult(failures.AsReadOnly());
    }

    private void OpenFile(string filePath)
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

        var choice = revitInfo.IsWorkshared ? _prompt.Ask(revitInfo) : WorksharedOpenChoice.Open;
        if (choice == WorksharedOpenChoice.Cancel)
        {
            return;
        }

        if (choice == WorksharedOpenChoice.Open)
        {
            _applicationLauncher.Launch(installation, filePath);
            return;
        }

        if (choice == WorksharedOpenChoice.CreateLocal && revitInfo.Worksharing.State != WorksharingState.Central)
        {
            throw new InvalidOperationException(
                $"Create new local needs a central model; '{revitInfo.Name}' is a {revitInfo.Worksharing.Text} model.");
        }

        if (!_addinLocator.IsInstalled(version))
        {
            var action = choice == WorksharedOpenChoice.Detach ? "Detach from central" : "Create new local";
            throw new InvalidOperationException(
                $"{action} needs the Revit Shell add-in for Revit {version}, which is not installed. Reinstall Revit Shell.");
        }

        _applicationLauncher.LaunchWithRequest(installation, choice, filePath);
    }
}
