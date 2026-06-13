namespace RevitShell.Domain;

/// <summary>
/// Represents an installed Revit application discovered on the current machine.
/// </summary>
public sealed class RevitInstallationInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RevitInstallationInfo"/> class.
    /// </summary>
    /// <param name="version">The Revit major version.</param>
    /// <param name="executablePath">The full path to <c>Revit.exe</c>.</param>
    public RevitInstallationInfo(int version, string executablePath)
    {
        Version = version;
        ExecutablePath = executablePath;
    }

    /// <summary>
    /// Gets the installed Revit major version.
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Gets the full path to the installed <c>Revit.exe</c>.
    /// </summary>
    public string ExecutablePath { get; }
}

