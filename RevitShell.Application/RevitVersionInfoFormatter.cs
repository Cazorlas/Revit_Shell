using System;
using System.Collections.Generic;
using System.Text;
using RevitShell.Domain;

namespace RevitShell.Application;

/// <summary>Formats inspected files for the version information dialog.</summary>
public static class RevitVersionInfoFormatter
{
    /// <summary>Builds file information blocks in the supplied order.</summary>
    /// <param name="infos">The inspected files to display.</param>
    /// <returns>The formatted dialog message.</returns>
    public static string Build(IEnumerable<RevitInfo> infos)
    {
        if (infos == null)
        {
            throw new ArgumentNullException(nameof(infos));
        }

        var builder = new StringBuilder();
        foreach (var info in infos)
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.AppendLine($"Name: {info.Name}\n");
            builder.AppendLine($"Path: {info.FilePath}\n");
            builder.AppendLine($"Version: {info.VersionText}");
            builder.Append($"Worksharing: {info.Worksharing.Text}");
            if (info.Worksharing.CentralModelPath != null)
            {
                builder.AppendLine();
                builder.Append($"Central model: {info.Worksharing.CentralModelPath}");
            }
        }

        return builder.ToString();
    }
}
