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

    /// <summary>Row 14: an automatic check is silent when the release is already installed on disk.</summary>
    [Fact]
    public void Run_InstalledReleaseAutomatic_IsSilentlyRestartPending()
    {
        var fixture = new UpdateFixture("1.4.0", installed: new Version(1, 4, 0));

        Assert.Equal(UpdateCheckOutcome.RestartPending, fixture.Run(false));

        Assert.Empty(fixture.Prompt.Asks);
        Assert.Empty(fixture.Prompt.UpToDateVersions);
        Assert.Empty(fixture.Prompt.RestartNeeded);
        Assert.Empty(fixture.Prompt.Installed);
        Assert.Empty(fixture.Prompt.Errors);
        Assert.Empty(fixture.Installer.Releases);
    }

    /// <summary>Row 15: a manual check reports the installed version that needs an Explorer restart.</summary>
    [Fact]
    public void Run_InstalledReleaseManual_ShowsRestartNeeded()
    {
        var fixture = new UpdateFixture("1.4.0", installed: new Version(1, 4, 0));

        Assert.Equal(UpdateCheckOutcome.RestartPending, fixture.Run(true));

        Assert.Equal(new Version(1, 4, 0), Assert.Single(fixture.Prompt.RestartNeeded));
        Assert.Empty(fixture.Prompt.Asks);
        Assert.Empty(fixture.Prompt.UpToDateVersions);
        Assert.Empty(fixture.Prompt.Installed);
        Assert.Empty(fixture.Prompt.Errors);
        Assert.Empty(fixture.Installer.Releases);
    }

    /// <summary>Row 16: a manual check reports a pending restart even when the feed has no release.</summary>
    [Fact]
    public void Run_InstalledVersionWithoutReleaseManual_ShowsRestartNeeded()
    {
        var fixture = new UpdateFixture(null, installed: new Version(1, 4, 0));

        Assert.Equal(UpdateCheckOutcome.RestartPending, fixture.Run(true));

        Assert.Equal(new Version(1, 4, 0), Assert.Single(fixture.Prompt.RestartNeeded));
        Assert.Empty(fixture.Prompt.Asks);
        Assert.Empty(fixture.Prompt.UpToDateVersions);
        Assert.Empty(fixture.Prompt.Installed);
        Assert.Empty(fixture.Prompt.Errors);
        Assert.Empty(fixture.Installer.Releases);
    }

    /// <summary>Row 17: a newer release is offered using the installed disk version as current.</summary>
    [Fact]
    public void Run_NewerThanDiskAutomatic_OffersReleaseWithDiskVersion()
    {
        var fixture = new UpdateFixture("1.5.0", installed: new Version(1, 4, 0));

        Assert.Equal(UpdateCheckOutcome.LaterChosen, fixture.Run(false));

        AssertAsked(fixture, new Version(1, 5, 0), new Version(1, 4, 0));
        Assert.Empty(fixture.Installer.Releases);
    }

    /// <summary>Row 18: choosing Install completes installation and reports the installed release.</summary>
    [Fact]
    public void Run_InstallChoice_InstallsOfferedRelease()
    {
        var fixture = new UpdateFixture("1.4.0", UpdatePromptChoice.Install);

        Assert.Equal(UpdateCheckOutcome.InstallCompleted, fixture.Run(false));

        var installed = Assert.Single(fixture.Installer.Releases);
        Assert.Same(fixture.Feed.Release, installed);
        Assert.Equal(new Version(1, 4, 0), installed.Version);
        Assert.Equal(new Version(1, 4, 0), Assert.Single(fixture.Prompt.Installed));
        Assert.Empty(fixture.Prompt.Errors);
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
    public void RegistryState_NewAndReopened_RoundTripsUtc()
    {
        var key = @"Software\PaperEngineer\ShellTests\" + Guid.NewGuid().ToString("N");
        try
        {
            var store = new RegistryUpdateStateStore(key);
            Assert.Null(store.LastCheckUtc);

            store.LastCheckUtc = Now;
            var reopened = new RegistryUpdateStateStore(key);

            Assert.Equal(Now, reopened.LastCheckUtc);
            Assert.Equal(DateTimeKind.Utc, reopened.LastCheckUtc!.Value.Kind);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(key, false);
        }
    }

    /// <summary>Row 30: verified MSI bytes run msiexec once and return Installed.</summary>
    [Fact]
    public void Install_ValidHash_DownloadsAndRunsMsi()
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
            }, info =>
            {
                starts.Add(info);
                return 0;
            }, directory);

            Assert.Equal(UpdateInstallResult.Installed, installer.Install(release));

            var download = Assert.Single(downloads);
            Assert.Equal(release.MsiUrl, download.Item1);
            Assert.Equal(path, download.Item2);
            var start = Assert.Single(starts);
            var expected = MsiUpdateInstaller.CreateStartInfo(path);
            Assert.Equal(expected.FileName, start.FileName);
            Assert.Equal(expected.Arguments, start.Arguments);
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
            }, info =>
            {
                starts.Add(info);
                return 0;
            }, directory);

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
                File.WriteAllBytes(target, Encoding.UTF8.GetBytes("abc")), info =>
                {
                    starts.Add(info);
                    return 0;
                }, directory);

            Assert.Throws<InvalidDataException>(() => installer.Install(release));

            Assert.Empty(starts);
        });
    }

    /// <summary>Row 33: msiexec runs a passive installation with the exact quoted path and no restart.</summary>
    [Fact]
    public void CreateStartInfo_MsiPath_SetsExactProcessOptions()
    {
        var start = MsiUpdateInstaller.CreateStartInfo(@"C:\t\P.msi");

        Assert.Equal(Path.Combine(Environment.SystemDirectory, "msiexec.exe"), start.FileName);
        Assert.Equal("/i \"C:\\t\\P.msi\" /passive /norestart", start.Arguments);
        Assert.True(start.UseShellExecute);
    }

    /// <summary>Row 34: a disk version older than the running version does not replace current.</summary>
    [Fact]
    public void Run_OlderDiskManual_ShowsRunningVersionUpToDate()
    {
        var fixture = new UpdateFixture("1.3.0", installed: new Version(1, 2, 0));

        Assert.Equal(UpdateCheckOutcome.UpToDate, fixture.Run(true));

        Assert.Equal(Current, Assert.Single(fixture.Prompt.UpToDateVersions));
        Assert.Empty(fixture.Prompt.Asks);
        Assert.Empty(fixture.Prompt.RestartNeeded);
        Assert.Empty(fixture.Prompt.Installed);
        Assert.Empty(fixture.Prompt.Errors);
    }

    /// <summary>Row 35: cancelling installation reports cancellation without installed feedback or errors.</summary>
    [Fact]
    public void Run_InstallerCancelled_DoesNotShowInstalledOrError()
    {
        var fixture = new UpdateFixture("1.4.0", UpdatePromptChoice.Install);
        fixture.Installer.Result = UpdateInstallResult.Cancelled;

        Assert.Equal(UpdateCheckOutcome.InstallCancelled, fixture.Run(false));

        Assert.Same(fixture.Feed.Release, Assert.Single(fixture.Installer.Releases));
        Assert.Empty(fixture.Prompt.Installed);
        Assert.Empty(fixture.Prompt.Errors);
    }

    /// <summary>Row 36: the update prompt offers exactly Install and Later.</summary>
    [Fact]
    public void UpdatePromptChoice_Names_ContainsOnlyInstallAndLater()
    {
        Assert.Equal(new[] { "Install", "Later" }, Enum.GetNames(typeof(UpdatePromptChoice)));
    }

    /// <summary>Row 37: successful MSI exit codes map to Installed.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(3010)]
    [InlineData(1641)]
    public void Install_SuccessExitCode_ReturnsInstalled(int exitCode)
    {
        WithDirectory(directory =>
        {
            var installer = new MsiUpdateInstaller((url, target) =>
                File.WriteAllBytes(target, Encoding.UTF8.GetBytes("abc")), info => exitCode, directory);

            Assert.Equal(UpdateInstallResult.Installed, installer.Install(Release("1.4.0", AbcHash)));
        });
    }

    /// <summary>Row 38: cancelled MSI exit codes map to Cancelled.</summary>
    [Theory]
    [InlineData(1602)]
    [InlineData(1223)]
    public void Install_CancelledExitCode_ReturnsCancelled(int exitCode)
    {
        WithDirectory(directory =>
        {
            var installer = new MsiUpdateInstaller((url, target) =>
                File.WriteAllBytes(target, Encoding.UTF8.GetBytes("abc")), info => exitCode, directory);

            Assert.Equal(UpdateInstallResult.Cancelled, installer.Install(Release("1.4.0", AbcHash)));
        });
    }

    /// <summary>Row 39: a failed MSI exit throws with the exit code in the message.</summary>
    [Fact]
    public void Install_FailureExitCode_ThrowsWithExitCode()
    {
        WithDirectory(directory =>
        {
            var installer = new MsiUpdateInstaller((url, target) =>
                File.WriteAllBytes(target, Encoding.UTF8.GetBytes("abc")), info => 1603, directory);

            var exception = Assert.Throws<InvalidOperationException>(() => installer.Install(Release("1.4.0", AbcHash)));

            Assert.Contains("1603", exception.Message);
        });
    }

    /// <summary>Row 40: a real assembly file supplies its three-component version.</summary>
    [Fact]
    public void InstalledVersion_RealAssembly_ReturnsThreeComponents()
    {
        var assembly = typeof(CheckForUpdatesUseCase).Assembly;
        var version = assembly.GetName().Version!;
        var source = new AssemblyFileVersionSource(assembly.Location);

        Assert.Equal(new Version(version.Major, version.Minor, version.Build), source.GetInstalledVersion());
    }

    /// <summary>Row 41: a missing assembly file returns null without throwing.</summary>
    [Fact]
    public void InstalledVersion_MissingFile_ReturnsNullWithoutThrowing()
    {
        WithDirectory(directory =>
        {
            var path = Path.Combine(directory, "missing.dll");
            Assert.False(File.Exists(path));
            var source = new AssemblyFileVersionSource(path);
            Version? version = null;

            var exception = Record.Exception(() => version = source.GetInstalledVersion());

            Assert.Null(exception);
            Assert.Null(version);
        });
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

    private static void AssertAsked(UpdateFixture fixture, Version version, Version? current = null)
    {
        var ask = Assert.Single(fixture.Prompt.Asks);
        Assert.Equal(current ?? Current, ask.Item1);
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
        public UpdateFixture(string? latestVersion, UpdatePromptChoice choice = UpdatePromptChoice.Later,
            Version? installed = null)
        {
            Feed.Release = latestVersion == null ? null : Release(latestVersion);
            Prompt.Choice = choice;
            UseCase = new CheckForUpdatesUseCase(Current, Feed, State, Prompt, Installer,
                new FakeInstalledVersionSource(installed), () => Now);
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
    }

    private sealed class RecordingPrompt : IUpdatePrompt
    {
        public UpdatePromptChoice Choice { get; set; }
        public List<Tuple<Version, UpdateRelease>> Asks { get; } = new List<Tuple<Version, UpdateRelease>>();
        public List<Version> UpToDateVersions { get; } = new List<Version>();
        public List<Version> RestartNeeded { get; } = new List<Version>();
        public List<Version> Installed { get; } = new List<Version>();
        public List<string> Errors { get; } = new List<string>();

        public UpdatePromptChoice Ask(Version current, UpdateRelease release)
        {
            Asks.Add(Tuple.Create(current, release));
            return Choice;
        }

        public void ShowUpToDate(Version current) => UpToDateVersions.Add(current);
        public void ShowRestartNeeded(Version installed) => RestartNeeded.Add(installed);
        public void ShowInstalled(Version installed) => Installed.Add(installed);
        public void ShowError(string message) => Errors.Add(message);
    }

    private sealed class FakeInstalledVersionSource : IInstalledVersionSource
    {
        private readonly Version? _installed;

        public FakeInstalledVersionSource(Version? installed) => _installed = installed;

        public Version? GetInstalledVersion() => _installed;
    }

    private sealed class RecordingInstaller : IUpdateInstaller
    {
        public List<UpdateRelease> Releases { get; } = new List<UpdateRelease>();
        public Exception? Error { get; set; }
        public UpdateInstallResult Result { get; set; } = UpdateInstallResult.Installed;

        public UpdateInstallResult Install(UpdateRelease release)
        {
            Releases.Add(release);
            if (Error != null)
            {
                throw Error;
            }
            return Result;
        }
    }
}
