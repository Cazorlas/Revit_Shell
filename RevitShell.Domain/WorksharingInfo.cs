namespace RevitShell.Domain;

/// <summary>
/// Represents worksharing metadata read from a Revit file.
/// </summary>
public sealed class WorksharingInfo
{
    /// <summary>Initializes worksharing information for a file.</summary>
    /// <param name="state">The file's worksharing role.</param>
    /// <param name="centralModelPath">The central model path, when available.</param>
    /// <param name="username">The recorded username, when available.</param>
    public WorksharingInfo(WorksharingState state, string? centralModelPath, string? username)
    {
        State = state;
        CentralModelPath = centralModelPath;
        Username = username;
    }

    /// <summary>Gets the file's worksharing role.</summary>
    public WorksharingState State { get; }

    /// <summary>Gets the central model path, when available.</summary>
    public string? CentralModelPath { get; }

    /// <summary>Gets the recorded username, when available.</summary>
    public string? Username { get; }

    /// <summary>Gets information for a file with an unknown worksharing state.</summary>
    public static WorksharingInfo Unknown { get; } = new WorksharingInfo(WorksharingState.Unknown, null, null);

    /// <summary>Gets the worksharing label used in the version information dialog.</summary>
    public string Text => State switch
    {
        WorksharingState.Central => "Central",
        WorksharingState.Local => "Local",
        WorksharingState.NotEnabled => "Not enabled",
        _ => "Unknown"
    };
}
