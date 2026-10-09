namespace RevitShell.Application;

/// <summary>Checks for the Revit Shell opening helper.</summary>
public interface IRevitAddinLocator
{
    /// <summary>Checks whether the helper is installed for the requested year.</summary>
    /// <param name="revitVersion">The exact Revit year.</param>
    /// <returns>Whether the helper manifest exists.</returns>
    bool IsInstalled(int revitVersion);
}
