using System;
using System.Text;
using System.Text.RegularExpressions;
using RevitShell.Domain;

namespace RevitShell.Infrastructure;

/// <summary>Decodes text labels from raw BasicFileInfo bytes.</summary>
public static class BasicFileInfoText
{
    private static readonly Regex VersionRegex = new Regex(@"\d{4}", RegexOptions.Compiled);

    /// <summary>Reads metadata at both possible UTF-16LE byte alignments.</summary>
    /// <param name="raw">The raw BasicFileInfo stream content.</param>
    /// <returns>The parsed metadata, with unknown or null values for absent labels.</returns>
    public static BasicFileInfoMetadata Parse(byte[] raw)
    {
        if (raw == null)
        {
            throw new ArgumentNullException(nameof(raw));
        }

        int? version = null;
        var worksharing = WorksharingState.Unknown;
        string? centralModelPath = null;
        string? username = null;

        // Binary headers can place the text at an odd offset in recent Revit files.
        for (var offset = 0; offset < 2 && offset < raw.Length; offset++)
        {
            var length = (raw.Length - offset) / 2 * 2;
            var text = Encoding.Unicode.GetString(raw, offset, length).Replace("\0", string.Empty);
            foreach (var line in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (TryGetValue(line, "Worksharing:", out var state))
                {
                    worksharing = string.Equals(state, "Central", StringComparison.OrdinalIgnoreCase)
                        ? WorksharingState.Central
                        : string.Equals(state, "Local", StringComparison.OrdinalIgnoreCase)
                            ? WorksharingState.Local
                            : string.Equals(state, "Not enabled", StringComparison.OrdinalIgnoreCase)
                                ? WorksharingState.NotEnabled
                                : WorksharingState.Unknown;
                }

                if (TryGetValue(line, "Username:", out var user))
                {
                    username = user.Length == 0 ? null : user;
                }

                if (TryGetValue(line, "Central Model Path:", out var path))
                {
                    centralModelPath = path.Length == 0 ? null : path;
                }

                if (TryGetValue(line, "Format:", out var format))
                {
                    var match = VersionRegex.Match(format);
                    if (match.Success && int.TryParse(match.Value, out var parsedVersion))
                    {
                        version = parsedVersion;
                    }
                }
            }
        }

        return new BasicFileInfoMetadata(version, worksharing, centralModelPath, username);
    }

    private static bool TryGetValue(string line, string label, out string value)
    {
        var index = line.IndexOf(label, StringComparison.OrdinalIgnoreCase);
        value = index < 0 ? string.Empty : line.Substring(index + label.Length).Trim();
        return index >= 0;
    }
}
