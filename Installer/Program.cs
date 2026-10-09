using System;
using System.IO;
using System.Linq;
using WixSharp;
using WixFile = WixSharp.File;

namespace Installer;

/// <summary>
/// Builds the MSI package for the PaperEngineer Shell extension.
/// </summary>
internal static class Program
{
    private const string ProductName = "PaperEngineer Shell";
    private const string CompanyName = "PaperEngineer";
    private static readonly Version ProductVersion = ReadProductVersion();
    private const string ShellExtensionClsid = "{7C7656C0-A90F-4B96-8B24-86C68A191F14}";
    private static readonly Guid ProductGuid = new Guid("057A74FC-01F8-49ED-AD21-78BF595F02BC");
    private static readonly (int Year, string TargetFramework)[] AddinTargets =
    {
        (2014, "net48"),
        (2015, "net48"),
        (2016, "net48"),
        (2017, "net48"),
        (2018, "net48"),
        (2019, "net48"),
        (2020, "net48"),
        (2021, "net48"),
        (2022, "net48"),
        (2023, "net48"),
        (2024, "net48"),
        (2025, "net8.0-windows"),
        (2026, "net8.0-windows"),
        (2027, "net10.0-windows")
    };

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
                new InstallDir(@"%ProgramFiles%\PaperEngineer\Shell",
                    installFiles),
                new Dir(@"%CommonAppDataFolder%\Autodesk\Revit\Addins",
                    GetAddinDirectories(solutionRoot, configuration)),
                new Dir(@"%ProgramMenu%\PaperEngineer\PaperEngineer Shell",
                    new ExeFileShortcut(
                        "Uninstall PaperEngineer Shell",
                        "[System64Folder]msiexec.exe",
                        "/x [ProductCode]")))
            {
                GUID = ProductGuid,
                Version = ProductVersion,
                Platform = Platform.x64,
                OutDir = outputDirectory,
                OutFileName = $"PaperEngineerShell_{ProductVersion}",
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
                        "PaperEngineer Shell Context Menu")
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

    private static WixEntity[] GetAddinDirectories(string solutionRoot, string configuration)
    {
        return AddinTargets.Select(target =>
        {
            var sourceDirectory = Path.Combine(solutionRoot, "RevitShell.RevitAddin", "bin", configuration, target.TargetFramework);
            var manifestPath = Path.Combine(sourceDirectory, "RevitShell.OpenHelper.addin");
            var assemblyPath = Path.Combine(sourceDirectory, "RevitShell.RevitAddin.dll");

            foreach (var path in new[] { manifestPath, assemblyPath })
            {
                if (!System.IO.File.Exists(path))
                {
                    throw new FileNotFoundException($"Revit {target.Year} add-in payload was not found in the {configuration} output: {path}", path);
                }
            }

            var assemblyFiles = new[] { assemblyPath }
                .Concat(new[] { "RevitShell.RevitAddin.deps.json", "RevitShell.RevitAddin.runtimeconfig.json" }
                    .Select(name => Path.Combine(sourceDirectory, name))
                    .Where(System.IO.File.Exists))
                .Select(path => new WixFile(path)
                {
                    Id = new Id($"Addin{target.Year}{BuildFileId(path)}")
                })
                .Cast<WixEntity>()
                .ToArray();

            return (WixEntity)new Dir(target.Year.ToString(),
                new WixFile(manifestPath) { Id = new Id($"Addin{target.Year}Manifest") },
                new Dir("RevitShell", assemblyFiles));
        }).ToArray();
    }

    /// <summary>
    /// Reads the product version stamped on this assembly from the <c>Version</c> build property.
    /// </summary>
    /// <returns>The three-part product version used for the MSI and its file name.</returns>
    private static Version ReadProductVersion()
    {
        var version = typeof(Program).Assembly.GetName().Version ?? new Version(1, 0, 0);
        return new Version(version.Major, version.Minor, Math.Max(version.Build, 0));
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

