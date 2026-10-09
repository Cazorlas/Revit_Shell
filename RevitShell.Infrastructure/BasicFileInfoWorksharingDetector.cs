using RevitShell.Application;
using RevitShell.Domain;

namespace RevitShell.Infrastructure;

/// <summary>Detects worksharing by reading the file's BasicFileInfo stream.</summary>
public sealed class BasicFileInfoWorksharingDetector : IRevitWorksharingDetector
{
    /// <inheritdoc />
    public WorksharingInfo Detect(string path)
    {
        try
        {
            if (!BasicFileInfoStream.TryGetRawBasicFileInfo(path, out var raw))
            {
                return WorksharingInfo.Unknown;
            }

            var metadata = BasicFileInfoText.Parse(raw);
            return new WorksharingInfo(metadata.Worksharing, metadata.CentralModelPath, metadata.Username);
        }
        catch
        {
            return WorksharingInfo.Unknown;
        }
    }
}
