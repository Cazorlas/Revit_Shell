namespace RevitShell.Domain;

/// <summary>
/// Identifies the worksharing role of a Revit file.
/// </summary>
public enum WorksharingState
{
    /// <summary>Worksharing information is unavailable.</summary>
    Unknown,
    /// <summary>Worksharing is not enabled.</summary>
    NotEnabled,
    /// <summary>The file is a local model.</summary>
    Local,
    /// <summary>The file is a central model.</summary>
    Central
}
