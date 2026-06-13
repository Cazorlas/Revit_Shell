namespace RevitShell.Application;

/// <summary>
/// Defines a strategy for detecting the Revit version of a file.
/// </summary>
public interface IRevitVersionDetector
{
    /// <summary>
    /// Detects the Revit version for the specified file.
    /// </summary>
    /// <param name="path">The full path to the Revit file.</param>
    /// <returns>
    /// The detected Revit version, or <see langword="null"/> when the version cannot be determined.
    /// </returns>
    int? DetectVersion(string path);
}
