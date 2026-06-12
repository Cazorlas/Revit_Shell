namespace RevitShell.Core;

public interface IRevitVersionDetector
{
    int? DetectVersion(string path);
}
