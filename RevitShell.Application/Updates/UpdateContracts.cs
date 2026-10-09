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

/// <summary>Persists update timing and skipped versions.</summary>
public interface IUpdateStateStore
{
    /// <summary>Gets or sets the last feed check time in UTC.</summary>
    DateTime? LastCheckUtc { get; set; }
    /// <summary>Gets or sets the three-component version skipped by the user.</summary>
    string? SkippedVersion { get; set; }
}

/// <summary>Identifies the user's choice for an offered update.</summary>
public enum UpdatePromptChoice
{
    /// <summary>Install the offered release.</summary>
    Install,
    /// <summary>Skip automatic offers for this version.</summary>
    Skip,
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
    /// <summary>Reports an update failure.</summary>
    void ShowError(string message);
}

/// <summary>Downloads, verifies, and starts installation of a release.</summary>
public interface IUpdateInstaller
{
    /// <summary>Starts installation of the supplied release.</summary>
    void Install(UpdateRelease release);
}

/// <summary>Identifies the result of an update check.</summary>
public enum UpdateCheckOutcome
{
    /// <summary>The automatic check interval has not elapsed.</summary>
    NotDue,
    /// <summary>No newer eligible release is available.</summary>
    UpToDate,
    /// <summary>The available release was previously skipped.</summary>
    SkippedEarlier,
    /// <summary>The MSI installer was started.</summary>
    InstallStarted,
    /// <summary>The user skipped the offered version.</summary>
    SkipChosen,
    /// <summary>The user deferred the update.</summary>
    LaterChosen,
    /// <summary>The check or installation failed.</summary>
    Failed
}
