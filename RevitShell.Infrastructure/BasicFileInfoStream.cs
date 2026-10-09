using System;
using System.IO;
using System.IO.Packaging;

namespace RevitShell.Infrastructure;

/// <summary>Reads the shared BasicFileInfo stream used by metadata detectors.</summary>
internal static class BasicFileInfoStream
{
    private const string StreamName = "BasicFileInfo";

    internal static bool TryGetRawBasicFileInfo(string path, out byte[] rawData)
    {
        rawData = Array.Empty<byte>();
        if (!StructuredStorageUtils.IsFileStructuredStorage(path, false))
        {
            return false;
        }

        using (var storageRoot = new StructuredStorageRoot(path))
        {
            if (!storageRoot.BaseRoot.StreamExists(StreamName))
            {
                return false;
            }

            var streamInfo = storageRoot.BaseRoot.GetStreamInfo(StreamName);
            using (var stream = streamInfo.GetStream(FileMode.Open, FileAccess.Read))
            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                rawData = buffer.ToArray();
                return true;
            }
        }
    }
}
