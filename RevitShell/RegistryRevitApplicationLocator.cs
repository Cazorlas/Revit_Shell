using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RevitShell;

internal sealed class RegistryRevitApplicationLocator
{
    private const string RevitRootKey = @"SOFTWARE\Autodesk\Revit";

    public RevitInstallationInfo? FindBestMatch(int? requestedVersion)
    {
        var installations = FindInstallations();
        if (requestedVersion.HasValue)
        {
            var exactMatch = installations.FirstOrDefault(item => item.Version == requestedVersion.Value);
            if (exactMatch != null)
            {
                return exactMatch;
            }
        }

        return installations
            .OrderByDescending(item => item.Version)
            .FirstOrDefault();
    }

    private static List<RevitInstallationInfo> FindInstallations()
    {
        using var root = Registry.LocalMachine.OpenSubKey(RevitRootKey);
        if (root == null)
        {
            return new List<RevitInstallationInfo>();
        }

        return root
            .GetSubKeyNames()
            .Select(keyName => TryGetInstallation(root, keyName))
            .Where(info => info != null)
            .Cast<RevitInstallationInfo>()
            .OrderBy(item => item.Version)
            .ToList();
    }

    private static RevitInstallationInfo? TryGetInstallation(RegistryKey root, string keyName)
    {
        if (!int.TryParse(keyName, out var version))
        {
            return null;
        }

        using var versionKey = root.OpenSubKey(keyName);
        if (versionKey == null)
        {
            return null;
        }

        foreach (var subKeyName in versionKey.GetSubKeyNames().Where(name => name.StartsWith("REVIT-", System.StringComparison.OrdinalIgnoreCase)))
        {
            using var subKey = versionKey.OpenSubKey(subKeyName);
            var installationLocation = subKey?.GetValue("InstallationLocation") as string;
            if (string.IsNullOrWhiteSpace(installationLocation))
            {
                var installRoot = subKey?.GetValue("InstallRoot") as string;
                installationLocation = string.IsNullOrWhiteSpace(installRoot)
                    ? null
                    : Path.Combine(installRoot, $"Revit {version}");
            }

            var executablePath = TryGetExecutablePath(installationLocation);
            if (executablePath != null)
            {
                return new RevitInstallationInfo(version, executablePath);
            }
        }

        return null;
    }

    private static string? TryGetExecutablePath(string? installationLocation)
    {
        if (string.IsNullOrWhiteSpace(installationLocation))
        {
            return null;
        }

        var directory = installationLocation!;
        directory = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var exactPath = Path.Combine(directory, "Revit.exe");
        if (File.Exists(exactPath))
        {
            return exactPath;
        }

        return Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*revit*.exe").FirstOrDefault()
            : null;
    }
}
