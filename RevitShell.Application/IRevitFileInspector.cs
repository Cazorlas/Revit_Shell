using RevitShell.Domain;

namespace RevitShell.Application;

/// <summary>
/// Defines inspection behavior for Revit files.
/// </summary>
public interface IRevitFileInspector
{
    /// <summary>
    /// Determines whether the specified file path uses a supported Revit file extension.
    /// </summary>
    /// <param name="path">The file path to validate.</param>
    /// <returns><see langword="true"/> when the extension is supported; otherwise, <see langword="false"/>.</returns>
    bool IsSupportedFile(string path);

    /// <summary>
    /// Inspects the specified file and returns a normalized inspection result.
    /// </summary>
    /// <param name="path">The file path to inspect.</param>
    /// <returns>A <see cref="RevitInfo"/> describing the file state and detected version.</returns>
    RevitInfo Inspect(string path);
}
