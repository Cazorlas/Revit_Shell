using RevitShell.Domain;

namespace RevitShell.Application;

/// <summary>
/// Defines a strategy for locating installed Revit applications on the current machine.
/// </summary>
public interface IRevitInstallationLocator
{
    /// <summary>
    /// Finds the installed Revit application that exactly matches the requested version.
    /// </summary>
    /// <param name="requestedVersion">The required Revit major version.</param>
    /// <returns>
    /// A <see cref="RevitInstallationInfo"/> for the exact match, or <see langword="null"/> when no match is installed.
    /// </returns>
    RevitInstallationInfo? FindExactMatch(int requestedVersion);
}
