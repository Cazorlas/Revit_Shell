namespace RevitShell.RevitAddin.Requests;

/// <summary>
/// Specifies how Revit opens the requested model.
/// </summary>
public enum OpenRequestMode
{
    /// <summary>Opens the supplied model directly.</summary>
    Open,
    /// <summary>Detaches the model while preserving its worksets.</summary>
    Detach,
    /// <summary>Creates and opens a new local copy.</summary>
    CreateLocal
}
