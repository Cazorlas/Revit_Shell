using System;
using System.IO;
using System.Net;
using System.Text;
using RevitShell.Application;

namespace RevitShell.Infrastructure;

/// <summary>Retrieves the latest release through the GitHub API.</summary>
public sealed class GitHubUpdateFeed : IUpdateFeed
{
    /// <summary>Identifies the repository's latest-release endpoint.</summary>
    public static readonly Uri LatestReleaseUri = new Uri("https://api.github.com/repos/Cazorlas/Revit_Shell/releases/latest");

    private readonly Uri _endpoint;
    private readonly Version _currentVersion;
    private readonly int _timeoutMilliseconds;

    /// <summary>Initializes the endpoint, installed version, and request timeout.</summary>
    public GitHubUpdateFeed(Uri endpoint, Version currentVersion, TimeSpan timeout)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _currentVersion = currentVersion ?? throw new ArgumentNullException(nameof(currentVersion));
        if (timeout.TotalMilliseconds < 1 || timeout.TotalMilliseconds > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }
        _timeoutMilliseconds = (int)timeout.TotalMilliseconds;
    }

    /// <summary>Gets the latest eligible release, returning null when GitHub reports no release.</summary>
    public UpdateRelease? GetLatest()
    {
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        var request = (HttpWebRequest)WebRequest.Create(_endpoint);
        request.Method = "GET";
        request.UserAgent = "PaperEngineerShell/" + _currentVersion.ToString(3);
        request.Accept = "application/vnd.github+json";
        request.Timeout = _timeoutMilliseconds;
        request.ReadWriteTimeout = _timeoutMilliseconds;

        try
        {
            using var response = (HttpWebResponse)request.GetResponse();
            using var stream = response.GetResponseStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return GitHubReleaseParser.Parse(reader.ReadToEnd());
        }
        catch (WebException ex) when (ex.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.NotFound)
        {
            ex.Response.Dispose();
            return null;
        }
    }
}
