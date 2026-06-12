namespace RevitShell.Core;

public interface IRevitFileInspector
{
    bool IsSupportedFile(string path);

    RevitFileInspectionResult Inspect(string path);
}
