namespace RevitShell.Core;

public sealed class RevitFileInspectionResult
{
    public RevitFileInspectionResult(string filePath, bool exists, bool isSupported, int? version)
    {
        FilePath = filePath;
        Exists = exists;
        IsSupported = isSupported;
        Version = version;
    }

    public string FilePath { get; }

    public bool Exists { get; }

    public bool IsSupported { get; }

    public int? Version { get; }

    public string Description
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
