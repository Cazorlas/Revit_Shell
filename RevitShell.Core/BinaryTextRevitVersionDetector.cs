using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace RevitShell.Core;

public sealed class BinaryTextRevitVersionDetector : IRevitVersionDetector
{
    private static readonly Regex FormatRegex = new Regex(@"Format:\s*(\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex BuildRegex = new Regex(@"Revit Build:\s*Autodesk Revit\s*(\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex GenericVersionRegex = new Regex(@"Autodesk Revit\s*(\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
