using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.Win32;
using RevitShell.Application;
using RevitShell.Infrastructure;
using Xunit;

namespace RevitShell.Tests.Updates;

/// <summary>Verifies the update checker, release parser, state, and MSI contracts.</summary>
public sealed class UpdateTests
{
    private static readonly DateTime Now = new DateTime(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Version Current = new Version(1, 3, 0);
    private const string AbcHash = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
    private const string ReleaseHash = "71a580dcced4a15a16910de6b0b42edeedf75bfe40ae60877bd252bccc4519d3";

    /// <summary>Row 1: parses a three-component version.</summary>
    [Fact]
    public void TryParse_ThreeComponents_ReturnsVersion()
    {
        Assert.True(ReleaseVersion.TryParse("1.4.0", out var version));
        Assert.Equal(new Version(1, 4, 0), version);
    }

    /// <summary>Row 2: accepts a v-prefixed release tag.</summary>
    [Fact]
    public void TryParse_PrefixedTag_ReturnsVersion()
    {
        Assert.True(ReleaseVersion.TryParse("v1.4.0", out var version));
        Assert.Equal(new Version(1, 4, 0), version);
    }

    /// <summary>Row 3: normalizes a missing build component to zero.</summary>
    [Fact]
    public void TryParse_TwoComponents_NormalizesBuild()
    {
        Assert.True(ReleaseVersion.TryParse("1.4", out var version));
        Assert.Equal("1.4.0", version.ToString(3));
    }

    /// <summary>Row 4: rejects invalid, null, and empty versions.</summary>
    [Fact]
    public void TryParse_InvalidInputs_ReturnsFalse()
    {
        Assert.False(ReleaseVersion.TryParse("abc", out _));
        Assert.False(ReleaseVersion.TryParse(null, out _));
        Assert.False(ReleaseVersion.TryParse(string.Empty, out _));
    }

    /// <summary>Row 5: compares parsed versions numerically.</summary>
    [Fact]
    public void TryParse_DoubleDigitMinor_ComparesNumerically()
    {
        Assert.True(ReleaseVersion.TryParse("1.10.0", out var newer));
        Assert.True(ReleaseVersion.TryParse("1.9.0", out var older));
        Assert.True(newer > older);
    }

    /// <summary>Row 6: the first automatic check offers a newer release and records the check time.</summary>
    [Fact]
    public void Run_FirstAutomaticCheckLater_OffersReleaseAndRecordsTime()
    {
        var fixture = new UpdateFixture("1.4.0");

        Assert.Equal(UpdateCheckOutcome.LaterChosen, fixture.Run(false));

        AssertAsked(fixture, new Version(1, 4, 0));
        Assert.Equal(Now, fixture.State.LastCheckUtc);
        Assert.Null(fixture.State.SkippedVersion);
        Assert.Empty(fixture.Installer.Releases);
    }

    /// <summary>Row 7: an equal version is silently up to date on an automatic check.</summary>
    [Fact]
    public void Run_EqualAutomatic_IsSilentlyUpToDate()
    {
        var fixture = new UpdateFixture("1.3.0");

        Assert.Equal(UpdateCheckOutcome.UpToDate, fixture.Run(false));

        Assert.Empty(fixture.Prompt.Asks);
        Assert.Empty(fixture.Prompt.UpToDateVersions);
    }

    /// <summary>Row 8: an equal version shows up-to-date feedback on a manual check.</summary>
    [Fact]
    public void Run_EqualManual_ShowsUpToDate()
    {
        var fixture = new UpdateFixture("1.3.0");

        Assert.Equal(UpdateCheckOutcome.UpToDate, fixture.Run(true));

        Assert.Equal(Current, Assert.Single(fixture.Prompt.UpToDateVersions));
    }

    /// <summary>Row 9: an older release does not prompt for installation.</summary>
    [Fact]
    public void Run_OlderAutomatic_IsUpToDate()
    {
        var fixture = new UpdateFixture("1.2.0");

        Assert.Equal(UpdateCheckOutcome.UpToDate, fixture.Run(false));

        Assert.Empty(fixture.Prompt.Asks);
    }

    /// <summary>Row 10: a missing release shows up-to-date feedback for a manual check.</summary>
    [Fact]
    public void Run_NullReleaseManual_ShowsUpToDate()
    {
        var fixture = new UpdateFixture(null);

        Assert.Equal(UpdateCheckOutcome.UpToDate, fixture.Run(true));

        Assert.Equal(Current, Assert.Single(fixture.Prompt.UpToDateVersions));
    }

    /// <summary>Row 11: an automatic check within two hours does not query the feed or change the time.</summary>
    [Fact]
    public void Run_RecentAutomatic_IsNotDue()
    {
        var fixture = new UpdateFixture("1.4.0");
        var lastCheck = Now.AddHours(-2);
        fixture.State.LastCheckUtc = lastCheck;

        Assert.Equal(UpdateCheckOutcome.NotDue, fixture.Run(false));

        Assert.Equal(0, fixture.Feed.Calls);
        Assert.Equal(lastCheck, fixture.State.LastCheckUtc);
    }

    /// <summary>Row 12: an automatic check after twenty-five hours queries the feed.</summary>
    [Fact]
    public void Run_OldAutomaticCheck_QueriesFeed()
    {
        var fixture = new UpdateFixture("1.4.0");
        fixture.State.LastCheckUtc = Now.AddHours(-25);

        Assert.Equal(UpdateCheckOutcome.LaterChosen, fixture.Run(false));

        Assert.Equal(1, fixture.Feed.Calls);
    }

    /// <summary>Row 13: a manual check bypasses the recent-check delay.</summary>
    [Fact]
    public void Run_RecentManual_QueriesFeedAndAsks()
    {
        var fixture = new UpdateFixture("1.4.0");
        fixture.State.LastCheckUtc = Now.AddHours(-2);

        fixture.Run(true);

        Assert.Equal(1, fixture.Feed.Calls);
        AssertAsked(fixture, new Version(1, 4, 0));
    }

    /// <summary>Row 14: an automatic check does not offer the previously skipped version.</summary>
    [Fact]
    public void Run_SkippedAutomatic_DoesNotAsk()
    {
        var fixture = new UpdateFixture("1.4.0");
        fixture.State.SkippedVersion = "1.4.0";

        Assert.Equal(UpdateCheckOutcome.SkippedEarlier, fixture.Run(false));

        Assert.Empty(fixture.Prompt.Asks);
    }

    /// <summary>Row 15: a manual check offers the previously skipped version.</summary>
    [Fact]
    public void Run_SkippedManual_OffersAgain()
    {
        var fixture = new UpdateFixture("1.4.0");
        fixture.State.SkippedVersion = "1.4.0";

        Assert.Equal(UpdateCheckOutcome.LaterChosen, fixture.Run(true));

        AssertAsked(fixture, new Version(1, 4, 0));
    }

    /// <summary>Row 16: skipping one version does not suppress a newer version.</summary>
    [Fact]
    public void Run_NewerThanSkippedAutomatic_OffersRelease()
    {
        var fixture = new UpdateFixture("1.5.0");
        fixture.State.SkippedVersion = "1.4.0";

        fixture.Run(false);

        AssertAsked(fixture, new Version(1, 5, 0));
    }

    /// <summary>Row 17: choosing Skip stores the offered version without installing.</summary>
    [Fact]
    public void Run_SkipChoice_StoresSkippedVersion()
    {
        var fixture = new UpdateFixture("1.4.0", UpdatePromptChoice.Skip);

        Assert.Equal(UpdateCheckOutcome.SkipChosen, fixture.Run(false));

        Assert.Equal("1.4.0", fixture.State.SkippedVersion);
        Assert.Empty(fixture.Installer.Releases);
    }

    /// <summary>Row 18: choosing Install starts installation of the offered release.</summary>
    [Fact]
    public void Run_InstallChoice_InstallsOfferedRelease()
    {
        var fixture = new UpdateFixture("1.4.0", UpdatePromptChoice.Install);

        Assert.Equal(UpdateCheckOutcome.InstallStarted, fixture.Run(false));

        var installed = Assert.Single(fixture.Installer.Releases);
        Assert.Same(fixture.Feed.Release, installed);
        Assert.Equal(new Version(1, 4, 0), installed.Version);
    }

    /// <summary>Row 19: an installer hash failure shows an error without escaping.</summary>
    [Fact]
    public void Run_InstallerThrows_ReportsFailureWithoutThrowing()
    {
        var fixture = new UpdateFixture("1.4.0", UpdatePromptChoice.Install);
        fixture.Installer.Error = new InvalidDataException("hash mismatch");
        UpdateCheckOutcome? outcome = null;

        var exception = Record.Exception(() => outcome = fixture.Run(false));

        Assert.Null(exception);
        Assert.Equal(UpdateCheckOutcome.Failed, outcome);
        Assert.Contains("hash mismatch", Assert.Single(fixture.Prompt.Errors));
    }

    /// <summary>Row 20: an automatic feed failure is silent and records the check time.</summary>
    [Fact]
    public void Run_FeedThrowsAutomatic_RecordsSilentFailure()
    {
        var fixture = new UpdateFixture(null);
        fixture.Feed.Error = new WebException("offline");
        UpdateCheckOutcome? outcome = null;

        var exception = Record.Exception(() => outcome = fixture.Run(false));

        Assert.Null(exception);
        Assert.Equal(UpdateCheckOutcome.Failed, outcome);
        Assert.Empty(fixture.Prompt.Asks);
        Assert.Empty(fixture.Prompt.UpToDateVersions);
        Assert.Empty(fixture.Prompt.Errors);
        Assert.Equal(Now, fixture.State.LastCheckUtc);
    }

    /// <summary>Row 21: a manual feed failure shows the network error.</summary>
    [Fact]
    public void Run_FeedThrowsManual_ShowsError()
    {
        var fixture = new UpdateFixture(null);
        fixture.Feed.Error = new WebException("offline");

        Assert.Equal(UpdateCheckOutcome.Failed, fixture.Run(true));

        Assert.Contains("offline", Assert.Single(fixture.Prompt.Errors));
    }

    /// <summary>Row 22: the real GitHub payload supplies the release version, MSI, digest, and page.</summary>
    [Fact]
    public void Parse_RealRelease_ReadsAllReleaseFields()
    {
        var release = GitHubReleaseParser.Parse(ReadFixture());

        Assert.NotNull(release);
        Assert.Equal(new Version(1, 3, 0), release!.Version);
        Assert.Equal("PaperEngineerShell_1.3.0.msi", release.MsiName);
        Assert.Equal(new Uri("https://github.com/Cazorlas/Revit_Shell/releases/download/1.3.0/PaperEngineerShell_1.3.0.msi"), release.MsiUrl);
        Assert.Equal(ReleaseHash, release.Sha256);
        Assert.Equal(new Uri("https://github.com/Cazorlas/Revit_Shell/releases/tag/1.3.0"), release.PageUrl);
    }

    /// <summary>Row 23: a prefixed tag in the real payload determines the release version.</summary>
    [Fact]
    public void Parse_PrefixedTag_ReadsVersionFromTag()
    {
        var json = ReplaceFixtureField("\"tag_name\":\"1.3.0\"", "\"tag_name\":\"v2.0.1\"");

        var release = GitHubReleaseParser.Parse(json);

        Assert.NotNull(release);
        Assert.Equal(new Version(2, 0, 1), release!.Version);
    }

    /// <summary>Row 24: a release with no MSI asset is ignored.</summary>
    [Fact]
    public void Parse_NonMsiAsset_ReturnsNull()
    {
        var json = ReplaceFixtureField("\"name\":\"PaperEngineerShell_1.3.0.msi\"", "\"name\":\"Other.zip\"");

        Assert.Null(GitHubReleaseParser.Parse(json));
    }

    /// <summary>Row 25: draft and prerelease payloads are each ignored.</summary>
    [Fact]
    public void Parse_DraftAndPrerelease_ReturnsNull()
    {
        var draft = ReplaceFixtureField("\"draft\":false", "\"draft\":true");
        var prerelease = ReplaceFixtureField("\"prerelease\":false", "\"prerelease\":true");

        Assert.Null(GitHubReleaseParser.Parse(draft));
        Assert.Null(GitHubReleaseParser.Parse(prerelease));
    }

    /// <summary>Row 26: a null digest still parses the release with a null hash.</summary>
    [Fact]
    public void Parse_NullDigest_ReturnsReleaseWithoutHash()
    {
        var json = ReplaceFixtureField("\"digest\":\"sha256:" + ReleaseHash + "\"", "\"digest\":null");

        var release = GitHubReleaseParser.Parse(json);

        Assert.NotNull(release);
        Assert.Null(release!.Sha256);
    }

    /// <summary>Row 27: malformed JSON throws while an empty object supplies no release.</summary>
    [Fact]
    public void Parse_MalformedAndEmptyJson_ThrowsOrReturnsNull()
    {
        Assert.ThrowsAny<Exception>(() => GitHubReleaseParser.Parse("not json"));
        Assert.Null(GitHubReleaseParser.Parse("{}"));
    }

    /// <summary>Row 28: SHA-256 uses file bytes and compares hashes without case sensitivity.</summary>
    [Fact]
    public void Sha256_AbcBytes_ComputesAndMatchesHash()
    {
        WithDirectory(directory =>
        {
            var path = Path.Combine(directory, "abc.txt");
            File.WriteAllBytes(path, Encoding.UTF8.GetBytes("abc"));

            Assert.Equal(AbcHash, Sha256Verifier.Compute(path));
            Assert.True(Sha256Verifier.Matches(path, "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD"));
            Assert.False(Sha256Verifier.Matches(path, "00"));
            Assert.False(Sha256Verifier.Matches(path, null));
        });
    }

    /// <summary>Row 29: registry state persists across instances and preserves UTC kind.</summary>
    [Fact]
    public void RegistryState_NewAndReopened_RoundTripsUtcAndSkippedVersion()
    {
        var key = @"Software\PaperEngineer\ShellTests\" + Guid.NewGuid().ToString("N");
        try
        {
            var store = new RegistryUpdateStateStore(key);
            Assert.Null(store.LastCheckUtc);
            Assert.Null(store.SkippedVersion);

            store.LastCheckUtc = Now;
            store.SkippedVersion = "1.4.0";
            var reopened = new RegistryUpdateStateStore(key);

            Assert.Equal(Now, reopened.LastCheckUtc);
            Assert.Equal(DateTimeKind.Utc, reopened.LastCheckUtc!.Value.Kind);
            Assert.Equal("1.4.0", reopened.SkippedVersion);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(key, false);
        }
    }

    /// <summary>Row 30: verified MSI bytes start msiexec once with the downloaded path.</summary>
    [Fact]
    public void Install_ValidHash_DownloadsAndStartsMsi()
    {
        WithDirectory(directory =>
        {
            var starts = new List<ProcessStartInfo>();
            var downloads = new List<Tuple<Uri, string>>();
            var release = Release("1.4.0", AbcHash);
            var path = Path.Combine(directory, release.MsiName);
            var installer = new MsiUpdateInstaller((url, target) =>
            {
                downloads.Add(Tuple.Create(url, target));
                File.WriteAllBytes(target, Encoding.UTF8.GetBytes("abc"));
            }, starts.Add, directory);

            installer.Install(release);

            var download = Assert.Single(downloads);
            Assert.Equal(release.MsiUrl, download.Item1);
            Assert.Equal(path, download.Item2);
            var start = Assert.Single(starts);
            Assert.Equal("cmd.exe", start.FileName);
            Assert.Contains("start \"\" /wait msiexec.exe /i \"" + path + "\" /passive /norestart", start.Arguments);
            Assert.True(File.Exists(path));
        });
    }

    /// <summary>Row 31: a hash mismatch prevents starting MSI and removes the download.</summary>
    [Fact]
    public void Install_MismatchedHash_ThrowsAndDeletesDownload()
    {
        WithDirectory(directory =>
        {
            var starts = new List<ProcessStartInfo>();
            var downloads = new List<string>();
            var release = Release("1.4.0", "00");
            var path = Path.Combine(directory, release.MsiName);
            var installer = new MsiUpdateInstaller((url, target) =>
            {
                downloads.Add(target);
                File.WriteAllBytes(target, Encoding.UTF8.GetBytes("abc"));
            }, starts.Add, directory);

            Assert.Throws<InvalidDataException>(() => installer.Install(release));

            Assert.Equal(path, Assert.Single(downloads));
            Assert.Empty(starts);
            Assert.False(File.Exists(path));
        });
    }

    /// <summary>Row 32: a missing release hash prevents starting MSI.</summary>
    [Fact]
    public void Install_NullHash_ThrowsWithoutStarting()
    {
        WithDirectory(directory =>
        {
            var starts = new List<ProcessStartInfo>();
            var release = Release("1.4.0", null);
            var installer = new MsiUpdateInstaller((url, target) =>
                File.WriteAllBytes(target, Encoding.UTF8.GetBytes("abc")), starts.Add, directory);

            Assert.Throws<InvalidDataException>(() => installer.Install(release));

            Assert.Empty(starts);
        });
    }

    /// <summary>
    /// Row 33: a hidden command waits for msiexec with the quoted path, then starts Explorer only when it is not running.
    /// </summary>
    [Fact]
    public void CreateStartInfo_MsiPath_SetsExactProcessOptions()
    {
        var start = MsiUpdateInstaller.CreateStartInfo(@"C:\t\P.msi");

        Assert.Equal("cmd.exe", start.FileName);
        Assert.Equal("/d /s /c \"start \"\" /wait msiexec.exe /i \"C:\\t\\P.msi\" /passive /norestart" +
            " & ping -n 4 127.0.0.1 >nul" +
            " & tasklist /fi \"imagename eq explorer.exe\" | find /i \"explorer.exe\" >nul || start \"\" explorer.exe\"",
            start.Arguments);
        Assert.True(start.UseShellExecute);
        Assert.Equal(ProcessWindowStyle.Hidden, start.WindowStyle);
    }

    private static UpdateRelease Release(string version, string? sha256 = "ab") =>
        new UpdateRelease(new Version(version), "PaperEngineerShell_" + version + ".msi",
            new Uri("https://example.test/PaperEngineerShell_" + version + ".msi"), sha256,
            new Uri("https://example.test/" + version));

    private static string ReadFixture() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Updates", "latest-release.json"));

    private static string ReplaceFixtureField(string before, string after)
    {
        var original = ReadFixture();
        Assert.Contains(before, original);
        var changed = original.Replace(before, after);
        Assert.NotEqual(original, changed);
        return changed;
    }

    private static void AssertAsked(UpdateFixture fixture, Version version)
    {
        var ask = Assert.Single(fixture.Prompt.Asks);
        Assert.Equal(Current, ask.Item1);
        Assert.Same(fixture.Feed.Release, ask.Item2);
        Assert.Equal(version, ask.Item2.Version);
    }

    private static void WithDirectory(Action<string> test)
    {
        var directory = Path.Combine(Path.GetTempPath(), "ShellUpdates-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(directory);
            test(directory);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    private sealed class UpdateFixture
    {
        public UpdateFixture(string? latestVersion, UpdatePromptChoice choice = UpdatePromptChoice.Later)
        {
            Feed.Release = latestVersion == null ? null : Release(latestVersion);
            Prompt.Choice = choice;
            UseCase = new CheckForUpdatesUseCase(Current, Feed, State, Prompt, Installer, () => Now);
        }

        public FakeFeed Feed { get; } = new FakeFeed();
        public MemoryState State { get; } = new MemoryState();
        public RecordingPrompt Prompt { get; } = new RecordingPrompt();
        public RecordingInstaller Installer { get; } = new RecordingInstaller();
        public CheckForUpdatesUseCase UseCase { get; }
        public UpdateCheckOutcome Run(bool manual) => UseCase.Run(manual);
    }

    private sealed class FakeFeed : IUpdateFeed
    {
        public UpdateRelease? Release { get; set; }
        public Exception? Error { get; set; }
        public int Calls { get; private set; }

        public UpdateRelease? GetLatest()
        {
            Calls++;
            if (Error != null)
            {
                throw Error;
            }
            return Release;
        }
    }

    private sealed class MemoryState : IUpdateStateStore
    {
        public DateTime? LastCheckUtc { get; set; }
        public string? SkippedVersion { get; set; }
    }

    private sealed class RecordingPrompt : IUpdatePrompt
    {
        public UpdatePromptChoice Choice { get; set; }
        public List<Tuple<Version, UpdateRelease>> Asks { get; } = new List<Tuple<Version, UpdateRelease>>();
        public List<Version> UpToDateVersions { get; } = new List<Version>();
        public List<string> Errors { get; } = new List<string>();

        public UpdatePromptChoice Ask(Version current, UpdateRelease release)
        {
            Asks.Add(Tuple.Create(current, release));
            return Choice;
        }

        public void ShowUpToDate(Version current) => UpToDateVersions.Add(current);
        public void ShowError(string message) => Errors.Add(message);
    }

    private sealed class RecordingInstaller : IUpdateInstaller
    {
        public List<UpdateRelease> Releases { get; } = new List<UpdateRelease>();
        public Exception? Error { get; set; }

        public void Install(UpdateRelease release)
        {
            Releases.Add(release);
            if (Error != null)
            {
                throw Error;
            }
        }
    }
}
