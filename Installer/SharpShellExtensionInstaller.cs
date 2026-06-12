using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Installer;

internal sealed class SharpShellExtensionInstaller
{
    private const string ProductFolderName = "RevitShell";
    private static readonly string[] RequiredFiles =
    {
        "RevitShell.dll",
        "RevitShell.Core.dll",
        "SharpShell.dll",
        "srm.exe"
    };

    public string InstallDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        ProductFolderName);

    private string InstalledServerPath => Path.Combine(InstallDirectory, "RevitShell.dll");

    private string SourceDirectory => AppContext.BaseDirectory;

    private string SourceSrmPath => Path.Combine(SourceDirectory, "srm.exe");

    private string InstalledSrmPath => Path.Combine(InstallDirectory, "srm.exe");

    public bool IsInstalled()
    {
        return File.Exists(InstalledServerPath);
    }

    public void Install()
    {
        RunSrm(SourceSrmPath, "uninstall", InstalledServerPath, ignoreExitCode: true);

        Directory.CreateDirectory(InstallDirectory);

        CopyFiles();
        RunSrm(SourceSrmPath, "install", InstalledServerPath, "-codebase", GetBitnessFlag());
    }

    public void Uninstall()
    {
        var srmPath = File.Exists(SourceSrmPath) ? SourceSrmPath : InstalledSrmPath;
        RunSrm(srmPath, "uninstall", InstalledServerPath, ignoreExitCode: true);

        foreach (var fileName in RequiredFiles)
        {
            var installedFile = Path.Combine(InstallDirectory, fileName);
            if (File.Exists(installedFile))
            {
                File.Delete(installedFile);
            }
        }

        if (Directory.Exists(InstallDirectory) && !Directory.EnumerateFileSystemEntries(InstallDirectory).Any())
        {
            Directory.Delete(InstallDirectory);
        }
    }

    private void CopyFiles()
    {
        foreach (var fileName in RequiredFiles)
        {
            var sourceFile = Path.Combine(SourceDirectory, fileName);
            if (!File.Exists(sourceFile))
            {
                throw new FileNotFoundException($"Required installer dependency was not found: {fileName}", sourceFile);
            }

            var targetFile = Path.Combine(InstallDirectory, fileName);
            File.Copy(sourceFile, targetFile, true);
        }
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
