namespace RevitShell.Application;

/// <summary>Identifies how a workshared model should be opened.</summary>
public enum WorksharedOpenChoice
{
    /// <summary>Opens the model directly.</summary>
    Open,
    /// <summary>Detaches the model from central.</summary>
    Detach,
    /// <summary>Creates a new local model from central.</summary>
    CreateLocal,
    /// <summary>Skips the model.</summary>
    Cancel
}
