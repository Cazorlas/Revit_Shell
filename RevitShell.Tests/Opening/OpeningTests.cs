using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RevitShell.Application;
using RevitShell.Domain;
using RevitShell.Infrastructure;
using Xunit;

namespace RevitShell.Tests.Opening;

/// <summary>Verifies the exact-version opening and request file contracts.</summary>
public sealed class OpeningTests
{
    private const string ModelPath = @"C:\m\A.rvt";

    /// <summary>Row 1: a non-workshared model opens without prompting.</summary>
    [Fact]
    public void Execute_NotEnabled_LaunchesDirectlyWithoutPrompt()
    {
        var fixture = new OpeningFixture(WorksharedOpenChoice.Detach, false, NotEnabled(ModelPath));

        var result = fixture.UseCase.Execute(new[] { ModelPath });

        AssertDirectLaunch(fixture.Launcher, ModelPath);
        Assert.Empty(fixture.Launcher.Requests);
        Assert.Equal(0, fixture.Prompt.Calls);
        Assert.Empty(result.Failures);
    }

    /// <summary>Row 2: detaching a central model uses an open request.</summary>
    [Fact]
    public void Execute_CentralDetach_LaunchesWithRequest()
    {
        var fixture = new OpeningFixture(WorksharedOpenChoice.Detach, true, Central(ModelPath));

        var result = fixture.UseCase.Execute(new[] { ModelPath });

        AssertRequestLaunch(fixture.Launcher, WorksharedOpenChoice.Detach);
        Assert.Equal(1, fixture.Prompt.Calls);
        Assert.Empty(result.Failures);
    }

    /// <summary>Row 3: creating a local from central uses an open request.</summary>
    [Fact]
    public void Execute_CentralCreateLocal_LaunchesWithRequest()
    {
        var fixture = new OpeningFixture(WorksharedOpenChoice.CreateLocal, true, Central(ModelPath));

        var result = fixture.UseCase.Execute(new[] { ModelPath });

        AssertRequestLaunch(fixture.Launcher, WorksharedOpenChoice.CreateLocal);
        Assert.Equal(1, fixture.Prompt.Calls);
        Assert.Empty(result.Failures);
    }

    /// <summary>Row 4: direct opening does not require the add-in.</summary>
    [Fact]
    public void Execute_CentralOpenWithoutAddin_LaunchesDirectly()
    {
        var fixture = new OpeningFixture(WorksharedOpenChoice.Open, false, Central(ModelPath));

        var result = fixture.UseCase.Execute(new[] { ModelPath });

        AssertDirectLaunch(fixture.Launcher, ModelPath);
        Assert.Empty(fixture.Launcher.Requests);
        Assert.Equal(1, fixture.Prompt.Calls);
        Assert.Empty(result.Failures);
    }

    /// <summary>Row 5: cancelling opens nothing and records no failure.</summary>
    [Fact]
    public void Execute_CentralCancel_DoesNotLaunchOrFail()
    {
        var fixture = new OpeningFixture(WorksharedOpenChoice.Cancel, false, Central(ModelPath));

        var result = fixture.UseCase.Execute(new[] { ModelPath });

        Assert.Empty(fixture.Launcher.DirectLaunches);
        Assert.Empty(fixture.Launcher.Requests);
        Assert.Equal(1, fixture.Prompt.Calls);
        Assert.Empty(result.Failures);
    }

    /// <summary>Row 6: detaching without the matching add-in records a failure.</summary>
    [Fact]
    public void Execute_CentralDetachWithoutAddin_ReportsRequiredAddinAndVersion()
    {
        var fixture = new OpeningFixture(WorksharedOpenChoice.Detach, false, Central(ModelPath));

        var result = fixture.UseCase.Execute(new[] { ModelPath });

        Assert.Empty(fixture.Launcher.DirectLaunches);
        Assert.Empty(fixture.Launcher.Requests);
        var failure = Assert.Single(result.Failures);
        Assert.Equal(ModelPath, failure.FilePath);
        Assert.Contains("add-in", failure.Message);
        Assert.Contains("2024", failure.Message);
    }

    /// <summary>Row 7: creating a local requires a central model.</summary>
    [Fact]
    public void Execute_LocalCreateLocal_ReportsCentralRequirement()
    {
        var local = new RevitInfo(ModelPath, true, true, 2024,
            new WorksharingInfo(WorksharingState.Local, @"\\srv\A.rvt", "user"));
        var fixture = new OpeningFixture(WorksharedOpenChoice.CreateLocal, true, local);

        var result = fixture.UseCase.Execute(new[] { ModelPath });

        Assert.Empty(fixture.Launcher.DirectLaunches);
        Assert.Empty(fixture.Launcher.Requests);
        var failure = Assert.Single(result.Failures);
        Assert.Equal(ModelPath, failure.FilePath);
        Assert.Contains("central", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Row 8: missing files and versions do not stop later files.</summary>
    [Fact]
    public void Execute_MissingAndUninstalledBeforeValid_CollectsFailuresAndContinues()
    {
        const string missingPath = @"C:\m\missing.rvt";
        const string oldPath = @"C:\m\old.rvt";
        var fixture = new OpeningFixture(WorksharedOpenChoice.Open, false,
            new RevitInfo(missingPath, false, true, null),
            new RevitInfo(oldPath, true, true, 2019,
                new WorksharingInfo(WorksharingState.NotEnabled, null, null)),
            NotEnabled(ModelPath));
        OpenRevitFilesResult? result = null;

        var exception = Record.Exception(() =>
            result = fixture.UseCase.Execute(new[] { missingPath, oldPath, ModelPath }));

        Assert.Null(exception);
        Assert.NotNull(result);
        Assert.Equal(2, result!.Failures.Count);
        Assert.Contains(result.Failures, failure => failure.FilePath == missingPath
            && failure.Message == "Revit file not found.");
        Assert.Contains(result.Failures, failure => failure.FilePath == oldPath
            && failure.Message == "Revit 2019 is not installed on this machine.");
        AssertDirectLaunch(fixture.Launcher, ModelPath);
        Assert.Empty(fixture.Launcher.Requests);
        Assert.Equal(0, fixture.Prompt.Calls);
    }

    /// <summary>Row 9: an empty selection remains an invalid operation.</summary>
    [Fact]
    public void Execute_EmptySelection_ThrowsInvalidOperationException()
    {
        var fixture = new OpeningFixture(WorksharedOpenChoice.Open, false);

        Assert.Throws<InvalidOperationException>(() => fixture.UseCase.Execute(new string[0]));
    }

    /// <summary>Row 10: request formatting preserves the exact CRLF contract.</summary>
    [Fact]
    public void Format_Detach_ReturnsExactRequestText()
    {
        var text = OpenRequestWriter.Format(WorksharedOpenChoice.Detach, ModelPath);

        Assert.Equal("RevitShellOpenRequest=1\r\nMode=Detach\r\nModelPath=C:\\m\\A.rvt\r\n", text);
    }

    /// <summary>Row 11: request writing uses a unique filename and UTF-8 without BOM.</summary>
    [Fact]
    public void Write_CreateLocal_WritesUtf8WithoutBomInsideRequestedDirectory()
    {
        WithDirectory(directory =>
        {
            var path = OpenRequestWriter.Write(directory, WorksharedOpenChoice.CreateLocal, ModelPath);

            Assert.Equal(Path.GetFullPath(directory), Path.GetDirectoryName(Path.GetFullPath(path)));
            Assert.StartsWith("open-", Path.GetFileName(path));
            Assert.EndsWith(".txt", Path.GetFileName(path));
            Assert.Equal(new UTF8Encoding(false).GetBytes(
                OpenRequestWriter.Format(WorksharedOpenChoice.CreateLocal, ModelPath)), File.ReadAllBytes(path));
        });
    }

    /// <summary>Row 12: the add-in locator checks the manifest for the exact year.</summary>
    [Fact]
    public void IsInstalled_ManifestFor2024_ReturnsTrueOnlyFor2024()
    {
        WithDirectory(directory =>
        {
            var yearDirectory = Path.Combine(directory, "2024");
            Directory.CreateDirectory(yearDirectory);
            File.WriteAllText(Path.Combine(yearDirectory, "RevitShell.OpenHelper.addin"), string.Empty);
            var locator = new ProgramDataRevitAddinLocator(directory);

            Assert.True(locator.IsInstalled(2024));
            Assert.False(locator.IsInstalled(2025));
        });
    }

    /// <summary>Row 13: request launches pass the environment variable without a file argument.</summary>
    [Fact]
    public void CreateRequestStartInfo_SetsExecutableEnvironmentAndWorkingDirectory()
    {
        var startInfo = ProcessRevitApplicationLauncher.CreateRequestStartInfo(
            new RevitInstallationInfo(2024, @"C:\R\2024\Revit.exe"), @"C:\t\open-1.txt");

        Assert.Equal(@"C:\R\2024\Revit.exe", startInfo.FileName);
        Assert.Equal(string.Empty, startInfo.Arguments);
        Assert.False(startInfo.UseShellExecute);
        Assert.Equal(@"C:\t\open-1.txt", startInfo.EnvironmentVariables["REVITSHELL_OPEN_REQUEST"]);
        Assert.Equal(@"C:\R\2024", startInfo.WorkingDirectory);
    }

    private static RevitInfo Central(string path) => new RevitInfo(path, true, true, 2024,
        new WorksharingInfo(WorksharingState.Central, @"\\srv\A.rvt", null));

    private static RevitInfo NotEnabled(string path) => new RevitInfo(path, true, true, 2024,
        new WorksharingInfo(WorksharingState.NotEnabled, null, null));

    private static void AssertDirectLaunch(RecordingLauncher launcher, string path)
    {
        var launch = Assert.Single(launcher.DirectLaunches);
        Assert.Equal(2024, launch.Item1.Version);
        Assert.Equal(@"C:\R\2024\Revit.exe", launch.Item1.ExecutablePath);
        Assert.Equal(path, launch.Item2);
    }

    private static void AssertRequestLaunch(RecordingLauncher launcher, WorksharedOpenChoice choice)
    {
        Assert.Empty(launcher.DirectLaunches);
        var request = Assert.Single(launcher.Requests);
        Assert.Equal(2024, request.Item1.Version);
        Assert.Equal(@"C:\R\2024\Revit.exe", request.Item1.ExecutablePath);
        Assert.Equal(choice, request.Item2);
        Assert.Equal(ModelPath, request.Item3);
    }

    private static void WithDirectory(Action<string> test)
    {
        var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Opening-" + Guid.NewGuid().ToString("N"));
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

    private sealed class OpeningFixture
    {
        public OpeningFixture(WorksharedOpenChoice choice, bool addinInstalled, params RevitInfo[] files)
        {
            Prompt = new FakePrompt(choice);
            UseCase = new OpenRevitFilesUseCase(new FakeInspector(files), new FakeInstallationLocator(),
                Launcher, Prompt, new FakeAddinLocator(addinInstalled ? new[] { 2024 } : new int[0]));
        }

        public RecordingLauncher Launcher { get; } = new RecordingLauncher();
        public FakePrompt Prompt { get; }
        public OpenRevitFilesUseCase UseCase { get; }
    }

    private sealed class FakeInspector : IRevitFileInspector
    {
        private readonly Dictionary<string, RevitInfo> _files = new Dictionary<string, RevitInfo>();

        public FakeInspector(IEnumerable<RevitInfo> files)
        {
            foreach (var file in files)
            {
                _files.Add(file.FilePath, file);
            }
        }

        public bool IsSupportedFile(string path) => _files[path].IsSupported;
        public RevitInfo Inspect(string path) => _files[path];
    }

    private sealed class FakeInstallationLocator : IRevitInstallationLocator
    {
        private readonly Dictionary<int, RevitInstallationInfo> _installations =
            new Dictionary<int, RevitInstallationInfo>
            {
                { 2024, new RevitInstallationInfo(2024, @"C:\R\2024\Revit.exe") }
            };

        public RevitInstallationInfo? FindExactMatch(int requestedVersion) =>
            _installations.TryGetValue(requestedVersion, out var installation) ? installation : null;
    }

    private sealed class RecordingLauncher : IRevitApplicationLauncher
    {
        public List<Tuple<RevitInstallationInfo, string>> DirectLaunches { get; } =
            new List<Tuple<RevitInstallationInfo, string>>();
        public List<Tuple<RevitInstallationInfo, WorksharedOpenChoice, string>> Requests { get; } =
            new List<Tuple<RevitInstallationInfo, WorksharedOpenChoice, string>>();

        public void Launch(RevitInstallationInfo installation, string filePath) =>
            DirectLaunches.Add(Tuple.Create(installation, filePath));

        public void LaunchWithRequest(RevitInstallationInfo installation, WorksharedOpenChoice choice,
            string filePath) => Requests.Add(Tuple.Create(installation, choice, filePath));
    }

    private sealed class FakePrompt : IWorksharedOpenPrompt
    {
        private readonly WorksharedOpenChoice _choice;

        public FakePrompt(WorksharedOpenChoice choice) => _choice = choice;
        public int Calls { get; private set; }

        public WorksharedOpenChoice Ask(RevitInfo info)
        {
            Calls++;
            return _choice;
        }
    }

    private sealed class FakeAddinLocator : IRevitAddinLocator
    {
        private readonly HashSet<int> _installedYears;

        public FakeAddinLocator(IEnumerable<int> installedYears) =>
            _installedYears = new HashSet<int>(installedYears);

        public bool IsInstalled(int revitVersion) => _installedYears.Contains(revitVersion);
    }
}
