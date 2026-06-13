using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Installer;

internal sealed class SharpShellExtensionInstaller
{
    private const string ServerFileName = "RevitShell.dll";
    private const string RegistrationManagerFileName = "srm.exe";

    public static void RegisterInstalledFiles(string installDirectory)
    {
        var serverPath = Path.Combine(installDirectory, ServerFileName);
        var srmPath = Path.Combine(installDirectory, RegistrationManagerFileName);

        RunSrm(srmPath, "uninstall", serverPath, ignoreExitCode: true);
        RunSrm(srmPath, "install", serverPath, "-codebase", GetBitnessFlag());
    }

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

    private static string GetBitnessFlag()
    {
        return Environment.Is64BitOperatingSystem ? "-os64" : "-os32";
    }
}
