using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using RevitShell.Application;

namespace RevitShell.Infrastructure;

/// <summary>Reads eligible MSI releases from GitHub release JSON.</summary>
public static class GitHubReleaseParser
{
    /// <summary>Parses the first matching MSI asset, ignoring drafts and prereleases.</summary>
    public static UpdateRelease? Parse(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var serializer = new DataContractJsonSerializer(typeof(ReleaseData));
        var data = (ReleaseData?)serializer.ReadObject(stream);
        if (data == null || data.Draft || data.Prerelease || !ReleaseVersion.TryParse(data.TagName, out var version))
        {
            return null;
        }

        var asset = data.Assets?.FirstOrDefault(item => item.Name != null &&
            Regex.IsMatch(item.Name, @"^PaperEngineerShell_.+\.msi$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
        if (asset == null)
        {
            return null;
        }

        var digest = asset.Digest;
        var sha256 = digest != null && Regex.IsMatch(digest, @"^sha256:[0-9a-fA-F]+$", RegexOptions.CultureInvariant)
            ? digest.Substring("sha256:".Length).ToLowerInvariant()
            : null;
        return new UpdateRelease(version, asset.Name!, new Uri(asset.DownloadUrl!), sha256, new Uri(data.HtmlUrl!));
    }

    [DataContract]
    private sealed class ReleaseData
    {
        /// <summary>Gets or sets the release tag.</summary>
        [DataMember(Name = "tag_name")]
        public string? TagName { get; set; }
        /// <summary>Gets or sets the release page.</summary>
        [DataMember(Name = "html_url")]
        public string? HtmlUrl { get; set; }
        /// <summary>Gets or sets whether the release is a draft.</summary>
        [DataMember(Name = "draft")]
        public bool Draft { get; set; }
        /// <summary>Gets or sets whether the release is a prerelease.</summary>
        [DataMember(Name = "prerelease")]
        public bool Prerelease { get; set; }
        /// <summary>Gets or sets the downloadable release assets.</summary>
        [DataMember(Name = "assets")]
        public AssetData[]? Assets { get; set; }
    }

    [DataContract]
    private sealed class AssetData
    {
        /// <summary>Gets or sets the asset filename.</summary>
        [DataMember(Name = "name")]
        public string? Name { get; set; }
        /// <summary>Gets or sets the asset download URI.</summary>
        [DataMember(Name = "browser_download_url")]
        public string? DownloadUrl { get; set; }
        /// <summary>Gets or sets the prefixed asset digest.</summary>
        [DataMember(Name = "digest")]
        public string? Digest { get; set; }
    }
}
