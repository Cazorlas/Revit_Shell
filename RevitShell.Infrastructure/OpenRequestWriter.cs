using System;
using System.IO;
using System.Text;
using RevitShell.Application;

namespace RevitShell.Infrastructure;

/// <summary>Writes requests consumed by the Revit Shell opening helper.</summary>
public static class OpenRequestWriter
{
    /// <summary>Formats a request with CRLF line endings.</summary>
    /// <param name="choice">The opening mode; cancellation is not a request.</param>
    /// <param name="filePath">The absolute model path.</param>
    /// <returns>The opening helper request text.</returns>
    public static string Format(WorksharedOpenChoice choice, string filePath)
    {
        if (choice != WorksharedOpenChoice.Open && choice != WorksharedOpenChoice.Detach
            && choice != WorksharedOpenChoice.CreateLocal)
        {
            throw new ArgumentException("The choice must be an opening mode.", nameof(choice));
        }

        return $"RevitShellOpenRequest=1\r\nMode={choice}\r\nModelPath={filePath}\r\n";
    }

    /// <summary>Creates the request directory and writes UTF-8 without a BOM.</summary>
    /// <param name="directory">The request directory.</param>
    /// <param name="choice">The opening mode.</param>
    /// <param name="filePath">The absolute model path.</param>
    /// <returns>The path to the unique request file.</returns>
    public static string Write(string directory, WorksharedOpenChoice choice, string filePath)
    {
        var content = Format(choice, filePath);
        Directory.CreateDirectory(directory);
        var requestPath = Path.Combine(directory, "open-" + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllText(requestPath, content, new UTF8Encoding(false));
        return requestPath;
    }
}
