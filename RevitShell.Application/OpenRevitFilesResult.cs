using System;
using System.Collections.Generic;

namespace RevitShell.Application;

/// <summary>Contains the failures collected while opening selected files.</summary>
public sealed class OpenRevitFilesResult
{
    /// <summary>Initializes the result with the collected failures.</summary>
    /// <param name="failures">The failures in selection order.</param>
    public OpenRevitFilesResult(IReadOnlyList<OpenRevitFileFailure> failures)
    {
        Failures = failures ?? throw new ArgumentNullException(nameof(failures));
    }

    /// <summary>Gets the failures in selection order.</summary>
    public IReadOnlyList<OpenRevitFileFailure> Failures { get; }
}

/// <summary>Describes why a selected file could not be opened.</summary>
public sealed class OpenRevitFileFailure
{
    /// <summary>Initializes a failure for one selected file.</summary>
    /// <param name="filePath">The selected file path.</param>
    /// <param name="message">The error to display.</param>
    public OpenRevitFileFailure(string filePath, string message)
    {
        FilePath = filePath;
        Message = message;
    }

    /// <summary>Gets the selected file path.</summary>
    public string FilePath { get; }

    /// <summary>Gets the error to display.</summary>
    public string Message { get; }
}
