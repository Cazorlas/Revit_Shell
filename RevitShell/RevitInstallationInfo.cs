namespace RevitShell;

internal sealed class RevitInstallationInfo
{
    public RevitInstallationInfo(int version, string executablePath)
    {
        Version = version;
        ExecutablePath = executablePath;
    }

    public int Version { get; }

    public string ExecutablePath { get; }
}

