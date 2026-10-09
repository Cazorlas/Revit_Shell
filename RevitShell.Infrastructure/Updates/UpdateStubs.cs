using System;
using System.Diagnostics;
using RevitShell.Application;

namespace RevitShell.Infrastructure;

public static class GitHubReleaseParser
{
    public static UpdateRelease? Parse(string json)
    {
        throw new NotImplementedException();
    }
}

public static class Sha256Verifier
{
    public static string Compute(string path)
    {
        throw new NotImplementedException();
    }

    public static bool Matches(string path, string? expectedHex)
    {
        throw new NotImplementedException();
    }
}

public sealed class RegistryUpdateStateStore : IUpdateStateStore
{
    public const string DefaultSubKey = @"Software\PaperEngineer\Shell";

    public RegistryUpdateStateStore(string subKeyPath)
    {
        throw new NotImplementedException();
    }

    public DateTime? LastCheckUtc
    {
        get { throw new NotImplementedException(); }
        set { throw new NotImplementedException(); }
    }

    public string? SkippedVersion
    {
        get { throw new NotImplementedException(); }
        set { throw new NotImplementedException(); }
    }
}

public sealed class MsiUpdateInstaller : IUpdateInstaller
{
    public MsiUpdateInstaller(Action<Uri, string> download, Action<ProcessStartInfo> start,
        string downloadDirectory)
    {
        throw new NotImplementedException();
    }

    public void Install(UpdateRelease release)
    {
        throw new NotImplementedException();
    }

    public static ProcessStartInfo CreateStartInfo(string msiPath)
    {
        throw new NotImplementedException();
    }
}
