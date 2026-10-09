using RevitShell.Domain;

namespace RevitShell.Application;

/// <summary>
/// Defines a strategy for launching a Revit file with a resolved Revit installation.
/// </summary>
public interface IRevitApplicationLauncher
{
    /// <summary>
    /// Launches the specified file with the supplied Revit installation.
    /// </summary>
    /// <param name="installation">The resolved installed Revit application.</param>
    /// <param name="filePath">The Revit file path to open.</param>
    void Launch(RevitInstallationInfo installation, string filePath);

    /// <summary>Launches Revit with a request for the opening helper.</summary>
    /// <param name="installation">The exact installed Revit application.</param>
    /// <param name="choice">The requested opening mode.</param>
    /// <param name="filePath">The model to open.</param>
    void LaunchWithRequest(RevitInstallationInfo installation, WorksharedOpenChoice choice, string filePath);
}
