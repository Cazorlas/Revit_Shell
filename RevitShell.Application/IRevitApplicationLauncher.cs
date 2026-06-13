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
}
