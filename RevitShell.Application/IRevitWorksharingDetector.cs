using RevitShell.Domain;

namespace RevitShell.Application;

/// <summary>Defines a strategy for reading worksharing metadata from a file.</summary>
public interface IRevitWorksharingDetector
{
    /// <summary>Detects the worksharing role and associated metadata.</summary>
    /// <param name="path">The Revit file path.</param>
    /// <returns>The detected information, or unknown when unavailable.</returns>
    WorksharingInfo Detect(string path);
}
