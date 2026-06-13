using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RevitShell.Application;
using RevitShell.Domain;

namespace RevitShell.Infrastructure;

/// <summary>
/// Resolves installed Revit applications from the Windows registry.
/// </summary>
public sealed class RegistryRevitInstallationLocator : IRevitInstallationLocator
{
    private const string RevitRootKey = @"SOFTWARE\Autodesk\Revit";

    /// <summary>
    /// Finds the installed Revit application that exactly matches the requested version.
    /// </summary>
    /// <param name="requestedVersion">The required Revit major version.</param>
    /// <returns>
    /// A <see cref="RevitInstallationInfo"/> for the exact match, or <see langword="null"/> when no match is installed.
    /// </returns>
    public RevitInstallationInfo? FindExactMatch(int requestedVersion)
    {
        var installations = FindInstallations();
        return installations.FirstOrDefault(item => item.Version == requestedVersion);
    }

    /// <summary>
    /// Enumerates all discoverable Revit installations from the registry.
    /// </summary>
    /// <returns>A list of discovered Revit installations sorted by version.</returns>
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

    /// <summary>
    /// Attempts to create an installation record from a Revit version registry key.
    /// </summary>
    /// <param name="root">The root Revit registry key.</param>
    /// <param name="keyName">The subkey name representing the Revit version.</param>
    /// <returns>A populated installation record, or <see langword="null"/> when the key cannot be resolved.</returns>
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

    /// <summary>
    /// Attempts to locate <c>Revit.exe</c> from a discovered installation directory.
    /// </summary>
    /// <param name="installationLocation">The installation directory reported by the registry.</param>
    /// <returns>The full executable path, or <see langword="null"/> when no executable is found.</returns>
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
