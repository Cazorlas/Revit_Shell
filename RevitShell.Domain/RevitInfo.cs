using System.IO;

namespace RevitShell.Domain;

/// <summary>
/// Represents normalized information about a Revit file.
/// </summary>
public sealed class RevitInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RevitInfo"/> class.
    /// </summary>
    /// <param name="filePath">The target file path.</param>
    /// <param name="exists">Indicates whether the file exists.</param>
    /// <param name="isSupported">Indicates whether the file extension is supported.</param>
    /// <param name="version">The detected Revit version, if available.</param>
    public RevitInfo(string filePath, bool exists, bool isSupported, int? version)
    {
        FilePath = filePath;
        Exists = exists;
        IsSupported = isSupported;
        Version = version;
    }

    /// <summary>
    /// Gets the target file path.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the file name portion of <see cref="FilePath"/>.
    /// </summary>
    public string Name => Path.GetFileName(FilePath);

    /// <summary>
    /// Gets the file extension portion of <see cref="FilePath"/>.
    /// </summary>
    public string Extension => Path.GetExtension(FilePath);

    /// <summary>
    /// Gets a value indicating whether the target file exists.
    /// </summary>
    public bool Exists { get; }

    /// <summary>
    /// Gets a value indicating whether the target file extension is supported.
    /// </summary>
    public bool IsSupported { get; }

    /// <summary>
    /// Gets the detected Revit major version, if available.
    /// </summary>
    public int? Version { get; }

    /// <summary>
    /// Gets a value indicating whether the file version was detected.
    /// </summary>
    public bool HasDetectedVersion => Version.HasValue;

    /// <summary>
    /// Gets a user-friendly version label for display.
    /// </summary>
    public string VersionText
    {
        get
        {
            if (!Exists)
            {
                return "File not found";
            }

            if (!IsSupported)
            {
                return "Unsupported extension";
            }

            return Version.HasValue ? $"Revit {Version.Value}" : "Version not detected";
        }
    }

    /// <summary>
    /// Gets a normalized status string for the file.
    /// </summary>
    public string Status
    {
        get
        {
            if (!Exists)
            {
                return "Missing";
            }

            return IsSupported ? "OK" : "Unsupported";
        }
    }
}
