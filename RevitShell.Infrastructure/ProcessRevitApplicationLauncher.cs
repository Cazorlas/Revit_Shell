using RevitShell.Application;
using RevitShell.Domain;
using System.Diagnostics;
using System.IO;

namespace RevitShell.Infrastructure;

/// <summary>
/// Launches Revit by starting the resolved <c>Revit.exe</c> process.
/// </summary>
public sealed class ProcessRevitApplicationLauncher : IRevitApplicationLauncher
{
    /// <inheritdoc />
    public void Launch(RevitInstallationInfo installation, string filePath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = installation.ExecutablePath,
            Arguments = $"\"{filePath}\"",
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(installation.ExecutablePath) ?? string.Empty
        });
    }
}
