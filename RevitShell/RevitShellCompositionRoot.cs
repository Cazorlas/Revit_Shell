using RevitShell.Application;
using RevitShell.Infrastructure;

namespace RevitShell;

/// <summary>
/// Centralizes runtime dependency wiring for the shell extension.
/// </summary>
internal static class RevitShellCompositionRoot
{
    /// <summary>
    /// Gets the file inspector used by the shell extension.
    /// </summary>
    public static IRevitFileInspector FileInspector { get; } = CreateFileInspector();

    /// <summary>
    /// Gets the use case that opens Revit files with their exact installed versions.
    /// </summary>
    public static OpenRevitFilesUseCase OpenRevitFiles { get; } = CreateOpenRevitFilesUseCase();

    private static IRevitFileInspector CreateFileInspector()
    {
        return new RevitFileInspector(
            new CompositeRevitVersionDetector(
                new BasicFileInfoRevitVersionDetector(),
                new BinaryTextRevitVersionDetector()));
    }

    private static OpenRevitFilesUseCase CreateOpenRevitFilesUseCase()
    {
        return new OpenRevitFilesUseCase(
            FileInspector,
            new RegistryRevitInstallationLocator(),
            new ProcessRevitApplicationLauncher());
    }
}
