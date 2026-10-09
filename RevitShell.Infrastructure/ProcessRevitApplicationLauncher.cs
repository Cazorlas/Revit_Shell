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
    public void LaunchWithRequest(RevitInstallationInfo installation, WorksharedOpenChoice choice, string filePath)
    {
        var requestPath = OpenRequestWriter.Write(Path.Combine(Path.GetTempPath(), "RevitShell"), choice, filePath);
        Process.Start(CreateRequestStartInfo(installation, requestPath));
    }

    /// <summary>Creates a Revit launch that passes the request through its environment.</summary>
    /// <param name="installation">The exact installed Revit application.</param>
    /// <param name="requestPath">The opening helper request file.</param>
    /// <returns>The process start information with no model argument.</returns>
    public static ProcessStartInfo CreateRequestStartInfo(RevitInstallationInfo installation, string requestPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = installation.ExecutablePath,
            Arguments = string.Empty,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(installation.ExecutablePath) ?? string.Empty
        };
        startInfo.EnvironmentVariables["REVITSHELL_OPEN_REQUEST"] = requestPath;
        return startInfo;
    }

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
