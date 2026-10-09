using System;
using RevitShell.Application;
using RevitShell.Infrastructure;

namespace RevitShell;

/// <summary>
/// Centralizes runtime dependency wiring for the shell extension.
/// </summary>
internal static class RevitShellCompositionRoot
{
    /// <summary>Gets the three-component version of the shell extension assembly.</summary>
    public static Version CurrentVersion { get; } = GetCurrentVersion();

    /// <summary>
    /// Gets the file inspector used by the shell extension.
    /// </summary>
    public static IRevitFileInspector FileInspector { get; } = CreateFileInspector();

    /// <summary>
    /// Gets the use case that opens Revit files with their exact installed versions.
    /// </summary>
    public static OpenRevitFilesUseCase OpenRevitFiles { get; } = CreateOpenRevitFilesUseCase();

    /// <summary>Gets the use case that checks for and offers shell extension updates.</summary>
    public static CheckForUpdatesUseCase CheckForUpdates { get; } = new CheckForUpdatesUseCase(
        CurrentVersion,
        new GitHubUpdateFeed(GitHubUpdateFeed.LatestReleaseUri, CurrentVersion, TimeSpan.FromSeconds(5)),
        new RegistryUpdateStateStore(RegistryUpdateStateStore.DefaultSubKey),
        new UpdateDialog(),
        new MsiUpdateInstaller(),
        () => DateTime.UtcNow);

    private static Version GetCurrentVersion()
    {
        var v = typeof(RevitShellCompositionRoot).Assembly.GetName().Version!;
        return new Version(v.Major, v.Minor, Math.Max(v.Build, 0));
    }

    private static IRevitFileInspector CreateFileInspector()
    {
        return new RevitFileInspector(
            new CompositeRevitVersionDetector(
                new BasicFileInfoRevitVersionDetector(),
                new BinaryTextRevitVersionDetector()),
            new BasicFileInfoWorksharingDetector());
    }

    private static OpenRevitFilesUseCase CreateOpenRevitFilesUseCase()
    {
        return new OpenRevitFilesUseCase(
            FileInspector,
            new RegistryRevitInstallationLocator(),
            new ProcessRevitApplicationLauncher(),
            new WorksharedOpenDialog(),
            new ProgramDataRevitAddinLocator());
    }
}
