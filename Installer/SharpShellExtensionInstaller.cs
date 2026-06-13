using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Installer;

/// <summary>
/// Provides helper methods for registering and unregistering the SharpShell extension on disk.
/// </summary>
internal sealed class SharpShellExtensionInstaller
{
    private const string ServerFileName = "RevitShell.dll";
    private const string RegistrationManagerFileName = "srm.exe";

    /// <summary>
    /// Registers the installed shell extension files from the specified installation directory.
    /// </summary>
    /// <param name="installDirectory">The installation directory containing <c>RevitShell.dll</c> and <c>srm.exe</c>.</param>
    public static void RegisterInstalledFiles(string installDirectory)
    {
        var serverPath = Path.Combine(installDirectory, ServerFileName);
        var srmPath = Path.Combine(installDirectory, RegistrationManagerFileName);

        RunSrm(srmPath, "uninstall", serverPath, ignoreExitCode: true);
        RunSrm(srmPath, "install", serverPath, "-codebase", GetBitnessFlag());
    }

    /// <summary>
    /// Unregisters the installed shell extension files from the specified installation directory.
    /// </summary>
    /// <param name="installDirectory">The installation directory containing <c>RevitShell.dll</c> and <c>srm.exe</c>.</param>
    public static void UnregisterInstalledFiles(string installDirectory)
    {
        var serverPath = Path.Combine(installDirectory, ServerFileName);
        var srmPath = Path.Combine(installDirectory, RegistrationManagerFileName);

        if (!File.Exists(serverPath) || !File.Exists(srmPath))
        {
            return;
        }

        RunSrm(srmPath, "uninstall", serverPath, ignoreExitCode: true);
    }

    /// <summary>
    /// Executes <c>srm.exe</c> with the supplied registration command.
    /// </summary>
    /// <param name="srmPath">The full path to <c>srm.exe</c>.</param>
    /// <param name="verb">The SRM verb, such as <c>install</c> or <c>uninstall</c>.</param>
    /// <param name="serverPath">The full path to the server assembly to register.</param>
    /// <param name="option">An optional command argument.</param>
    /// <param name="bitnessOption">An optional operating system bitness argument.</param>
    /// <param name="ignoreExitCode"><see langword="true"/> to ignore a non-zero exit code; otherwise, <see langword="false"/>.</param>
    private static void RunSrm(string srmPath, string verb, string serverPath, string option = "", string bitnessOption = "", bool ignoreExitCode = false)
    {
        if (!File.Exists(srmPath))
        {
            throw new FileNotFoundException("srm.exe was not found.", srmPath);
        }

        var arguments = string.Join(" ", new[]
        {
            verb,
            $"\"{serverPath}\"",
            option,
            bitnessOption
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = srmPath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(srmPath) ?? AppContext.BaseDirectory
        });

        if (process == null)
        {
            throw new InvalidOperationException("Failed to start srm.exe.");
        }

        process.WaitForExit();
        if (!ignoreExitCode && process.ExitCode != 0)
        {
            throw new InvalidOperationException($"SharpShell registration failed with exit code {process.ExitCode}.");
        }
    }

    /// <summary>
    /// Gets the SRM operating system bitness flag for the current machine.
    /// </summary>
    /// <returns><c>-os64</c> for 64-bit Windows; otherwise, <c>-os32</c>.</returns>
    private static string GetBitnessFlag()
    {
        return Environment.Is64BitOperatingSystem ? "-os64" : "-os32";
    }
}
