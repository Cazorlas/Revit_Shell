namespace RevitShell.RevitAddin.Requests;

/// <summary>
/// Contains the mode and model path supplied by the shell.
/// </summary>
public sealed class OpenRequest
{
    /// <summary>Initializes a model open request.</summary>
    /// <param name="mode">The requested open mode.</param>
    /// <param name="modelPath">The absolute model path.</param>
    public OpenRequest(OpenRequestMode mode, string modelPath)
    {
        Mode = mode;
        ModelPath = modelPath;
    }

    /// <summary>Gets the requested open mode.</summary>
    public OpenRequestMode Mode { get; }

    /// <summary>Gets the absolute model path.</summary>
    public string ModelPath { get; }
}
