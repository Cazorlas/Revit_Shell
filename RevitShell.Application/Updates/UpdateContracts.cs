using System;

namespace RevitShell.Application;

/// <summary>Parses release tags into numerically comparable versions.</summary>
public static class ReleaseVersion
{
    /// <summary>Parses an optional v prefix and normalizes a missing build to zero.</summary>
    public static bool TryParse(string? text, out Version version)
    {
        version = new Version(0, 0, 0);
        if (text == null || text.Length == 0)
        {
            return false;
        }

        var tag = text.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? text.Substring(1) : text;
        if (!Version.TryParse(tag, out var parsed))
        {
            return false;
        }

        version = parsed.Build < 0 ? new Version(parsed.Major, parsed.Minor, 0) : parsed;
        return true;
    }
}

/// <summary>Describes a release and its downloadable MSI.</summary>
public sealed class UpdateRelease
{
    /// <summary>Initializes the release version, asset, digest, and release page.</summary>
    public UpdateRelease(Version version, string msiName, Uri msiUrl, string? sha256, Uri pageUrl)
    {
        Version = version ?? throw new ArgumentNullException(nameof(version));
        MsiName = msiName ?? throw new ArgumentNullException(nameof(msiName));
        MsiUrl = msiUrl ?? throw new ArgumentNullException(nameof(msiUrl));
        Sha256 = sha256;
        PageUrl = pageUrl ?? throw new ArgumentNullException(nameof(pageUrl));
    }

    /// <summary>Gets the release version.</summary>
    public Version Version { get; }
    /// <summary>Gets the MSI asset filename.</summary>
    public string MsiName { get; }
    /// <summary>Gets the MSI download URI.</summary>
    public Uri MsiUrl { get; }
    /// <summary>Gets the expected SHA-256 digest, when supplied.</summary>
    public string? Sha256 { get; }
    /// <summary>Gets the release page URI.</summary>
    public Uri PageUrl { get; }
}

/// <summary>Retrieves the latest available release.</summary>
public interface IUpdateFeed
{
    /// <summary>Gets the latest release or null when no eligible release exists.</summary>
    UpdateRelease? GetLatest();
}

/// <summary>Reads the shell extension version installed on disk.</summary>
public interface IInstalledVersionSource
{
    /// <summary>Gets the installed version or null when it cannot be read.</summary>
    Version? GetInstalledVersion();
}

/// <summary>Persists update timing.</summary>
public interface IUpdateStateStore
{
    /// <summary>Gets or sets the last feed check time in UTC.</summary>
    DateTime? LastCheckUtc { get; set; }
}

/// <summary>Identifies the user's choice for an offered update.</summary>
public enum UpdatePromptChoice
{
    /// <summary>Install the offered release.</summary>
    Install,
    /// <summary>Defer installation.</summary>
    Later
}

/// <summary>Shows update choices and feedback.</summary>
public interface IUpdatePrompt
{
    /// <summary>Asks how to handle an available release.</summary>
    UpdatePromptChoice Ask(Version current, UpdateRelease release);
    /// <summary>Reports that the installed version is up to date.</summary>
    void ShowUpToDate(Version current);
    /// <summary>Reports that the installed version needs an Explorer restart.</summary>
    void ShowRestartNeeded(Version installed);
    /// <summary>Reports successful installation and the required Explorer restart.</summary>
    void ShowInstalled(Version installed);
    /// <summary>Reports an update failure.</summary>
    void ShowError(string message);
}

/// <summary>Identifies the result of installing a release.</summary>
public enum UpdateInstallResult
{
    /// <summary>The release was installed.</summary>
    Installed,
    /// <summary>The user cancelled installation.</summary>
    Cancelled
}

/// <summary>Downloads, verifies, and installs a release.</summary>
public interface IUpdateInstaller
{
    /// <summary>Installs the supplied release and returns the installer result.</summary>
    UpdateInstallResult Install(UpdateRelease release);
}

/// <summary>Identifies the result of an update check.</summary>
public enum UpdateCheckOutcome
{
    /// <summary>The automatic check interval has not elapsed.</summary>
    NotDue,
    /// <summary>No newer eligible release is available.</summary>
    UpToDate,
    /// <summary>A newer installed version needs an Explorer restart.</summary>
    RestartPending,
    /// <summary>The MSI installation completed.</summary>
    InstallCompleted,
    /// <summary>The user cancelled MSI installation.</summary>
    InstallCancelled,
    /// <summary>The user deferred the update.</summary>
    LaterChosen,
    /// <summary>The check or installation failed.</summary>
    Failed
}
