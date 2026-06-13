using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using RevitShell.Application;

namespace RevitShell.Infrastructure;

/// <summary>
/// Detects a Revit file version by scanning the file header for recognizable text markers.
/// </summary>
public sealed class BinaryTextRevitVersionDetector : IRevitVersionDetector
{
    private static readonly Regex FormatRegex = new Regex(@"Format:\s*(\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex BuildRegex = new Regex(@"Revit Build:\s*Autodesk Revit\s*(\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex GenericVersionRegex = new Regex(@"Autodesk Revit\s*(\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <inheritdoc />
    public int? DetectVersion(string path)
    {
        using var stream = File.OpenRead(path);
        var bytesToRead = (int)Math.Min(stream.Length, 262_144);
        var buffer = new byte[bytesToRead];
        var read = stream.Read(buffer, 0, buffer.Length);

        var unicodeText = Encoding.Unicode.GetString(buffer, 0, read);
        var asciiText = Encoding.ASCII.GetString(buffer, 0, read);
        var text = unicodeText + Environment.NewLine + asciiText;

        return ExtractVersion(text);
    }

    /// <summary>
    /// Extracts a Revit version from the combined decoded text.
    /// </summary>
    /// <param name="text">The decoded text content to search.</param>
    /// <returns>
    /// The detected Revit version, or <see langword="null"/> when no known marker is found.
    /// </returns>
    private static int? ExtractVersion(string text)
    {
        var match = FormatRegex.Match(text);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var formatVersion))
        {
            return formatVersion;
        }

        match = BuildRegex.Match(text);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var buildVersion))
        {
            return buildVersion;
        }

        match = GenericVersionRegex.Match(text);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var genericVersion))
        {
            return genericVersion;
        }

        return null;
    }
}
