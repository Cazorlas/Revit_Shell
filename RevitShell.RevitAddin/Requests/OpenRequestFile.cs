using System;
using System.IO;
using System.Text;

namespace RevitShell.RevitAddin.Requests;

/// <summary>Reads and consumes shell model open requests.</summary>
public static class OpenRequestFile
{
    /// <summary>Names the environment variable containing the request file path.</summary>
    public const string EnvironmentVariable = "REVITSHELL_OPEN_REQUEST";

    /// <summary>Reads a request and makes a best-effort attempt to delete it after reading.</summary>
    /// <param name="path">The request file path.</param>
    /// <param name="request">The parsed request, or null when reading or validation fails.</param>
    /// <returns>True when the file contains a valid request.</returns>
    public static bool TryRead(string path, out OpenRequest? request)
    {
        request = null;
        string[] lines;
        try
        {
            lines = File.ReadAllLines(path, Encoding.UTF8);
        }
        catch (Exception)
        {
            // A missing or unreadable request must not interrupt normal Revit startup.
            return false;
        }

        try
        {
            File.Delete(path);
        }
        catch (Exception)
        {
            // Consumption is best effort; deletion failure does not invalidate the content.
        }

        string? header = null;
        string? modeText = null;
        string? modelPath = null;
        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimStart('\uFEFF');
            var separator = line.IndexOf('=');
            if (separator < 0)
            {
                continue;
            }

            var key = line.Substring(0, separator);
            var value = line.Substring(separator + 1);
            if (key.Equals("RevitShellOpenRequest", StringComparison.OrdinalIgnoreCase))
                header = value;
            else if (key.Equals("Mode", StringComparison.OrdinalIgnoreCase))
                modeText = value;
            else if (key.Equals("ModelPath", StringComparison.OrdinalIgnoreCase))
                modelPath = value;
        }

        if (header != "1" || modelPath == null || string.IsNullOrWhiteSpace(modelPath))
        {
            return false;
        }

        OpenRequestMode mode;
        if (string.Equals(modeText, "Open", StringComparison.OrdinalIgnoreCase))
            mode = OpenRequestMode.Open;
        else if (string.Equals(modeText, "Detach", StringComparison.OrdinalIgnoreCase))
            mode = OpenRequestMode.Detach;
        else if (string.Equals(modeText, "CreateLocal", StringComparison.OrdinalIgnoreCase))
            mode = OpenRequestMode.CreateLocal;
        else
            return false;

        try
        {
            // A drive-relative path such as C:model.rvt is not an absolute model path.
            if (!Path.IsPathRooted(modelPath) || (Path.GetPathRoot(modelPath)?.Length ?? 0) < 3 ||
                !string.Equals(Path.GetExtension(modelPath), ".rvt", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        catch (ArgumentException)
        {
            return false;
        }

        request = new OpenRequest(mode, modelPath);
        return true;
    }
}
