using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using RevitShell.Application;

namespace RevitShell.Infrastructure;

/// <summary>Downloads and verifies a release MSI before starting Windows Installer.</summary>
public sealed class MsiUpdateInstaller : IUpdateInstaller
{
    private readonly Action<Uri, string> _download;
    private readonly Func<ProcessStartInfo, int> _run;
    private readonly string _downloadDirectory;

    /// <summary>Initializes downloads and process launching in the temporary updates directory.</summary>
    public MsiUpdateInstaller()
        : this(Download, info =>
            {
                using var process = Process.Start(info) ?? throw new InvalidOperationException("Windows Installer did not start.");
                process.WaitForExit();
                return process.ExitCode;
            },
            Path.Combine(Path.GetTempPath(), "PaperEngineerShell", "Updates"))
    {
    }

    /// <summary>Initializes the downloader, process launcher, and destination directory.</summary>
    public MsiUpdateInstaller(Action<Uri, string> download, Func<ProcessStartInfo, int> run,
        string dir)
    {
        _download = download ?? throw new ArgumentNullException(nameof(download));
        _run = run ?? throw new ArgumentNullException(nameof(run));
        _downloadDirectory = dir ?? throw new ArgumentNullException(nameof(dir));
    }

    /// <summary>Downloads the MSI, rejects unverified bytes, and waits for installation.</summary>
    public UpdateInstallResult Install(UpdateRelease release)
    {
        if (release == null)
        {
            throw new ArgumentNullException(nameof(release));
        }
        if (Path.GetFileName(release.MsiName) != release.MsiName ||
            release.MsiName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new InvalidDataException("Invalid MSI filename: " + release.MsiName + ".");
        }

        Directory.CreateDirectory(_downloadDirectory);
        var path = Path.Combine(_downloadDirectory, release.MsiName);
        _download(release.MsiUrl, path);
        if (!Sha256Verifier.Matches(path, release.Sha256))
        {
            File.Delete(path);
            throw new InvalidDataException("SHA-256 verification failed for " + release.MsiName + ".");
        }
        var exitCode = _run(CreateStartInfo(path));
        switch (exitCode)
        {
            case 0:
            case 3010:
            case 1641:
                return UpdateInstallResult.Installed;
            case 1602:
            case 1223:
                return UpdateInstallResult.Cancelled;
            default:
                throw new InvalidOperationException("Windows Installer exited with code " + exitCode + ".");
        }
    }

    /// <summary>Creates a passive Windows Installer command without restarting Windows.</summary>
    public static ProcessStartInfo CreateStartInfo(string msiPath)
    {
        return new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "msiexec.exe"),
            Arguments = "/i \"" + msiPath + "\" /passive /norestart",
            UseShellExecute = true
        };
    }

    private static void Download(Uri url, string path)
    {
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        using var client = new WebClient();
        client.DownloadFile(url, path);
    }
}
