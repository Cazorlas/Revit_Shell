using System;

namespace RevitShell.Application;

/// <summary>Coordinates update checks without letting exceptions escape into Explorer.</summary>
public sealed class CheckForUpdatesUseCase
{
    private readonly Version _current;
    private readonly IUpdateFeed _feed;
    private readonly IUpdateStateStore _state;
    private readonly IUpdatePrompt _prompt;
    private readonly IUpdateInstaller _installer;
    private readonly Func<DateTime> _utcNow;

    /// <summary>Initializes the update checker with its feed, state, prompt, installer, and clock.</summary>
    public CheckForUpdatesUseCase(Version current, IUpdateFeed feed, IUpdateStateStore state,
        IUpdatePrompt prompt, IUpdateInstaller installer, Func<DateTime> utcNow)
    {
        _current = current ?? throw new ArgumentNullException(nameof(current));
        _feed = feed ?? throw new ArgumentNullException(nameof(feed));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
        _installer = installer ?? throw new ArgumentNullException(nameof(installer));
        _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
    }

    /// <summary>Checks for updates, bypassing timing and skip rules for manual checks.</summary>
    public UpdateCheckOutcome Run(bool manual)
    {
        var installing = false;
        try
        {
            if (!manual)
            {
                var lastCheck = _state.LastCheckUtc;
                if (lastCheck.HasValue && _utcNow() - lastCheck.Value < TimeSpan.FromHours(24))
                {
                    return UpdateCheckOutcome.NotDue;
                }
            }

            UpdateRelease? release;
            try
            {
                release = _feed.GetLatest();
            }
            finally
            {
                _state.LastCheckUtc = _utcNow();
            }

            if (release == null || release.Version <= _current)
            {
                if (manual)
                {
                    _prompt.ShowUpToDate(_current);
                }
                return UpdateCheckOutcome.UpToDate;
            }

            if (!manual && _state.SkippedVersion == release.Version.ToString(3))
            {
                return UpdateCheckOutcome.SkippedEarlier;
            }

            switch (_prompt.Ask(_current, release))
            {
                case UpdatePromptChoice.Skip:
                    _state.SkippedVersion = release.Version.ToString(3);
                    return UpdateCheckOutcome.SkipChosen;
                case UpdatePromptChoice.Install:
                    installing = true;
                    _installer.Install(release);
                    return UpdateCheckOutcome.InstallStarted;
                default:
                    return UpdateCheckOutcome.LaterChosen;
            }
        }
        catch (Exception ex)
        {
            if (manual || installing)
            {
                try
                {
                    _prompt.ShowError("Update failed: " + ex.Message);
                }
                catch (Exception)
                {
                    // Feedback must not let a dialog failure escape into the COM host.
                }
            }
            return UpdateCheckOutcome.Failed;
        }
    }
}
