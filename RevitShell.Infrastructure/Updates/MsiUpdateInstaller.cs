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
    private readonly Action<ProcessStartInfo> _start;
    private readonly string _downloadDirectory;

    /// <summary>Initializes downloads and process launching in the temporary updates directory.</summary>
    public MsiUpdateInstaller()
        : this(Download, info => { Process.Start(info); },
            Path.Combine(Path.GetTempPath(), "PaperEngineerShell", "Updates"))
    {
    }

    /// <summary>Initializes the downloader, process launcher, and destination directory.</summary>
    public MsiUpdateInstaller(Action<Uri, string> download, Action<ProcessStartInfo> start,
        string downloadDirectory)
    {
        _download = download ?? throw new ArgumentNullException(nameof(download));
        _start = start ?? throw new ArgumentNullException(nameof(start));
        _downloadDirectory = downloadDirectory ?? throw new ArgumentNullException(nameof(downloadDirectory));
    }

    /// <summary>Downloads the MSI, rejects unverified bytes, and starts installation.</summary>
    public void Install(UpdateRelease release)
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
        _start(CreateStartInfo(path));
    }

    /// <summary>Creates a shell launch for passive installation without restarting Windows.</summary>
    public static ProcessStartInfo CreateStartInfo(string msiPath)
    {
        return new ProcessStartInfo
        {
            FileName = "msiexec.exe",
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
