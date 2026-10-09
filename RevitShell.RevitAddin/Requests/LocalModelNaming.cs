using System;
using System.Globalization;
using System.IO;

namespace RevitShell.RevitAddin.Requests;

/// <summary>Names local model copies and backups without using the Revit API.</summary>
public static class LocalModelNaming
{
    /// <summary>Builds a local model path in the user's documents directory.</summary>
    /// <param name="modelPath">The central model path.</param>
    /// <param name="username">The Revit username.</param>
    /// <param name="documentsDirectory">The user's documents directory.</param>
    /// <returns>The model filename with a sanitized username suffix.</returns>
    public static string GetLocalPath(string modelPath, string username, string documentsDirectory)
    {
        var sanitized = username.ToCharArray();
        var invalidCharacters = Path.GetInvalidFileNameChars();
        for (var index = 0; index < sanitized.Length; index++)
        {
            if (Array.IndexOf(invalidCharacters, sanitized[index]) >= 0)
            {
                sanitized[index] = '_';
            }
        }

        return Path.Combine(documentsDirectory,
            Path.GetFileNameWithoutExtension(modelPath) + "_" + new string(sanitized) + ".rvt");
    }

    /// <summary>Builds a timestamped backup path alongside the local model.</summary>
    /// <param name="localPath">The existing local model path.</param>
    /// <param name="timestamp">The time used in the backup filename.</param>
    /// <returns>The timestamped backup path.</returns>
    public static string GetBackupPath(string localPath, DateTime timestamp)
    {
        return Path.Combine(Path.GetDirectoryName(localPath)!,
            Path.GetFileNameWithoutExtension(localPath) + "_backup_" +
            timestamp.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".rvt");
    }
}
