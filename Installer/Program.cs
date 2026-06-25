using System;
using System.IO;
using System.Linq;
using WixSharp;
using WixFile = WixSharp.File;

namespace Installer;

/// <summary>
/// Builds the MSI package for the Revit Shell extension.
/// </summary>
internal static class Program
{
    private const string ProductName = "Revit Shell";
    private const string CompanyName = "PaperEngineer";
    private const string ProductVersion = "1.0.0";
    private const string ShellExtensionClsid = "{7C7656C0-A90F-4B96-8B24-86C68A191F14}";
    private static readonly Guid ProductGuid = new Guid("057A74FC-01F8-49ED-AD21-78BF595F02BC");

    /// <summary>
    /// Builds the MSI package for the requested configuration.
    /// </summary>
    /// <param name="args">
    /// Optional command-line arguments where the first argument is the build configuration, such as <c>Release</c>.
    /// </param>
    /// <returns><c>0</c> when the MSI build succeeds; otherwise, a non-zero exit code.</returns>
    private static int Main(string[] args)
    {
        try
        {
            var configuration = args.Length > 0 ? args[0] : "Release";
            var solutionRoot = ResolveSolutionRoot();
            var payloadDirectory = Path.Combine(solutionRoot, "RevitShell", "bin", configuration, "net48");
            var srmPath = Path.Combine(AppContext.BaseDirectory, "srm.exe");
            var outputDirectory = Path.Combine(solutionRoot, "Installer", "bin", configuration, "msi");
            var installerAssetDirectory = Path.Combine(solutionRoot, "sources", "installer");
            var licenceFile = Path.Combine(installerAssetDirectory, "LICENSE.rtf");
            var bannerImage = Path.Combine(installerAssetDirectory, "installer-banner.bmp");
            var backgroundImage = Path.Combine(installerAssetDirectory, "installer-background.bmp");
            var srmFile = new WixFile(srmPath)
            {
                Id = new Id("SrmExe")
            };

            var payloadFiles = GetPayloadFiles(payloadDirectory);
            var installFiles = payloadFiles.Concat(new WixEntity[] { srmFile }).ToArray();

            var project = new Project(
                ProductName,
                new InstallDir(@"%ProgramFiles%\RevitShell",
                    installFiles),
                new Dir(@"%ProgramMenu%\PaperEngineer\Revit Shell",
                    new ExeFileShortcut(
                        "Uninstall Revit Shell",
                        "[System64Folder]msiexec.exe",
                        "/x [ProductCode]")))
            {
                GUID = ProductGuid,
                Version = new Version(ProductVersion),
                Platform = Platform.x64,
                OutDir = outputDirectory,
                OutFileName = "RevitShell",
                InstallScope = InstallScope.perMachine,
                UI = WUI.WixUI_Minimal,
                LicenceFile = licenceFile,
                BannerImage = bannerImage,
                BackgroundImage = backgroundImage,
                MajorUpgrade = MajorUpgrade.Default,
                RegValues = new[]
                {
                    new RegValue(
                        RegistryHive.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved",
                        ShellExtensionClsid,
                        "Revit Shell Context Menu")
                    {
                        Win64 = true
                    }
                },
                ControlPanelInfo =
                {
                    Manufacturer = CompanyName,
                    InstallLocation = "[INSTALLDIR]",
                    NoModify = true,
                    NoRepair = true
                },
                Actions = new WixSharp.Action[]
                {
                    new InstalledFileAction(
                        "SrmExe",
                        "install \"[INSTALLDIR]RevitShell.dll\" -codebase -os64",
                        Return.check,
                        When.After,
                        Step.InstallFiles,
                        Condition.NOT_Installed)
                    {
                        Execute = Execute.deferred,
                        Impersonate = false
                    },
                    new InstalledFileAction(
                        "SrmExe",
                        "uninstall \"[INSTALLDIR]RevitShell.dll\"",
                        Return.ignore,
                        When.Before,
                        Step.RemoveFiles,
                        new Condition("REMOVE=\"ALL\""))
                    {
                        Execute = Execute.deferred,
                        Impersonate = false
                    }
                }
            };

            MajorUpgrade.Default.AllowSameVersionUpgrades = true;
            project.LightOptions += " -sice:ICE30 -sice:ICE60 -sice:ICE61 -sice:ICE80 -sice:ICE91";

            Directory.CreateDirectory(outputDirectory);
            project.BuildMsi();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    /// <summary>
    /// Collects the payload DLLs that should be packaged into the MSI.
    /// </summary>
    /// <param name="payloadDirectory">The RevitShell output directory for the selected build configuration.</param>
    /// <returns>An array of WiX entities representing the payload files.</returns>
    private static WixEntity[] GetPayloadFiles(string payloadDirectory)
    {
        var requiredFiles = Directory
            .EnumerateFiles(payloadDirectory, "*.dll", SearchOption.TopDirectoryOnly)
            .Select(path => new WixFile(path)
            {
                Id = new Id(BuildFileId(path))
            })
            .Cast<WixEntity>()
            .ToList();

        if (!requiredFiles.Any(file => file is WixFile wixFile && Path.GetFileName(wixFile.Name).Equals("RevitShell.dll", StringComparison.OrdinalIgnoreCase)))
        {
            throw new FileNotFoundException("RevitShell.dll was not found in the release output.", Path.Combine(payloadDirectory, "RevitShell.dll"));
        }

        return requiredFiles.ToArray();
    }

    /// <summary>
    /// Builds a stable WiX file identifier from a file name.
    /// </summary>
    /// <param name="path">The payload file path.</param>
    /// <returns>A sanitized WiX identifier.</returns>
    private static string BuildFileId(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        var sanitized = new string(fileName.Where(char.IsLetterOrDigit).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "PayloadFile" : sanitized;
    }

    /// <summary>
    /// Resolves the solution root by walking upward from the current output directory.
    /// </summary>
    /// <returns>The directory that contains <c>RevitShell.sln</c>.</returns>
    private static string ResolveSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            if (System.IO.File.Exists(Path.Combine(directory.FullName, "RevitShell.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find RevitShell.sln from the installer output directory.");
    }
}

