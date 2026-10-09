using RevitShell.Domain;

namespace RevitShell.Infrastructure;

/// <summary>Represents text metadata from the BasicFileInfo stream.</summary>
public sealed class BasicFileInfoMetadata
{
    internal BasicFileInfoMetadata(int? version, WorksharingState worksharing, string? centralModelPath, string? username)
    {
        Version = version;
        Worksharing = worksharing;
        CentralModelPath = centralModelPath;
        Username = username;
    }

    /// <summary>Gets the detected Revit major version.</summary>
    public int? Version { get; }

    /// <summary>Gets the detected worksharing role.</summary>
    public WorksharingState Worksharing { get; }

    /// <summary>Gets the central model path, when present.</summary>
    public string? CentralModelPath { get; }

    /// <summary>Gets the recorded username, when present.</summary>
    public string? Username { get; }
}
