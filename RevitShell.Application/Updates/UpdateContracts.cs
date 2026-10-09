using System;

namespace RevitShell.Application;

public static class ReleaseVersion
{
    public static bool TryParse(string? text, out Version version)
    {
        throw new NotImplementedException();
    }
}

public sealed class UpdateRelease
{
    public UpdateRelease(Version version, string msiName, Uri msiUrl, string? sha256, Uri pageUrl)
    {
        throw new NotImplementedException();
    }

    public Version Version { get { throw new NotImplementedException(); } }
    public string MsiName { get { throw new NotImplementedException(); } }
    public Uri MsiUrl { get { throw new NotImplementedException(); } }
    public string? Sha256 { get { throw new NotImplementedException(); } }
    public Uri PageUrl { get { throw new NotImplementedException(); } }
}

public interface IUpdateFeed
{
    UpdateRelease? GetLatest();
}

public interface IUpdateStateStore
{
    DateTime? LastCheckUtc { get; set; }
    string? SkippedVersion { get; set; }
}

public enum UpdatePromptChoice { Install, Skip, Later }

public interface IUpdatePrompt
{
    UpdatePromptChoice Ask(Version current, UpdateRelease release);
    void ShowUpToDate(Version current);
    void ShowError(string message);
}

public interface IUpdateInstaller
{
    void Install(UpdateRelease release);
}

public enum UpdateCheckOutcome
{
    NotDue, UpToDate, SkippedEarlier, InstallStarted, SkipChosen, LaterChosen, Failed
}

public sealed class CheckForUpdatesUseCase
{
    public CheckForUpdatesUseCase(Version current, IUpdateFeed feed, IUpdateStateStore state,
        IUpdatePrompt prompt, IUpdateInstaller installer, Func<DateTime> utcNow)
    {
        throw new NotImplementedException();
    }

    public UpdateCheckOutcome Run(bool manual)
    {
        throw new NotImplementedException();
    }
}
