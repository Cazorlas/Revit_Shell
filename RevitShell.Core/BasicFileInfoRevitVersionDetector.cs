using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RevitShell.Core;

public sealed class BasicFileInfoRevitVersionDetector : IRevitVersionDetector
{
    private const string StreamName = "BasicFileInfo";
    private static readonly byte[] VersionMarker = { 0x04, 0x00, 0x00, 0x00 };
    private static readonly Encoding UnicodeEncoding = Encoding.Unicode;
    private static readonly Regex FormatRegex = new Regex(@"^Format:.*?(\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex BuildRegex = new Regex(@"^Revit Build:.*?(\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public int? DetectVersion(string path)
    {
        try
        {
            if (!TryGetRawBasicFileInfo(path, out var rawData))
            {
                return null;
            }

            return TryExtractVersion(rawData);
        }
        catch
        {
            return null;
        }
    }

    private static int? TryExtractVersion(byte[] rawData)
    {
        foreach (var line in GetCandidateLines(rawData))
        {
            if (TryMatchVersion(line, out var version))
            {
                return version;
            }
        }

        return TryParseBinaryVersion(rawData);
    }

    private static string[] GetCandidateLines(byte[] rawData)
    {
        var utf8Text = Encoding.UTF8.GetString(rawData).Replace("\0", string.Empty);
        var unicodeText = UnicodeEncoding.GetString(rawData).Replace("\0", string.Empty);

        return (utf8Text + Environment.NewLine + unicodeText)
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToArray();
    }

    private static bool TryMatchVersion(string input, out int version)
    {
        version = 0;

        var buildMatch = BuildRegex.Match(input);
        if (buildMatch.Success && int.TryParse(buildMatch.Groups[1].Value, out version))
        {
            return true;
        }

        var formatMatch = FormatRegex.Match(input);
        if (formatMatch.Success && int.TryParse(formatMatch.Groups[1].Value, out version))
        {
            return true;
        }

        return false;
    }

    private static bool TryGetRawBasicFileInfo(string path, out byte[] rawData)
    {
        rawData = Array.Empty<byte>();

        if (!StructuredStorageUtils.IsFileStructuredStorage(path, false))
        {
            return false;
        }

        using (var storageRoot = new StructuredStorageRoot(path))
        {
            if (!storageRoot.BaseRoot.StreamExists(StreamName))
            {
                return false;
            }

            var streamInfo = storageRoot.BaseRoot.GetStreamInfo(StreamName);
            using (var stream = streamInfo.GetStream(FileMode.Open, FileAccess.Read))
            {
                rawData = new byte[stream.Length];
                stream.Read(rawData, 0, rawData.Length);
                return true;
            }
        }
    }

    private static int? TryParseBinaryVersion(byte[] data)
    {
        if (data.Length < sizeof(int))
        {
            return null;
        }

        var fileVersion = BitConverter.ToInt32(data, 0);
        switch (fileVersion)
        {
            case 10:
                return ParseVersion10(data);
            case 13:
            case 14:
                return ParseVersion13Or14(data);
            default:
                return null;
        }
    }

    private static int? ParseVersion10(byte[] data)
    {
        if (!TryReadUnicodeString(data, 14, out var versionText))
        {
            return null;
        }

        if (versionText.Length < 19)
        {
            return null;
        }

        var digits = new string(versionText.Skip(15).Take(4).ToArray());
        return int.TryParse(digits, out var version) ? version : null;
    }

    private static int? ParseVersion13Or14(byte[] data)
    {
        var markerIndex = FindVersionMarker(data);
        if (markerIndex < 0 || markerIndex + 8 > data.Length)
        {
            return null;
        }

        var versionText = UnicodeEncoding.GetString(data, markerIndex, 8).TrimEnd('\0', ' ');
        return int.TryParse(versionText, out var version) ? version : null;
    }

    private static int FindVersionMarker(byte[] data)
    {
        for (var index = 0; index <= data.Length - VersionMarker.Length; index++)
        {
            var matched = true;
            for (var offset = 0; offset < VersionMarker.Length; offset++)
            {
                if (data[index + offset] != VersionMarker[offset])
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                return index + VersionMarker.Length;
            }
        }

        return -1;
    }

    private static bool TryReadUnicodeString(byte[] data, int position, out string value)
    {
        value = string.Empty;

        if (position < 0 || position + sizeof(int) > data.Length)
        {
            return false;
        }

        var length = BitConverter.ToInt32(data, position);
        if (length < 0)
        {
            return false;
        }

        var start = position + sizeof(int);
        var byteLength = checked(length * 2);
        if (start + byteLength > data.Length)
        {
            return false;
        }

        value = UnicodeEncoding.GetString(data, start, byteLength);
        return true;
    }
}
