using System;
using System.IO;
using System.Text;
using RevitShell.Application;
using RevitShell.Domain;
using RevitShell.Infrastructure;
using Xunit;

namespace RevitShell.Tests.Worksharing;

/// <summary>
/// Verifies the worksharing metadata, display, and inspection contract.
/// </summary>
public sealed class WorksharingTests
{
    /// <summary>
    /// Contract row 1: reads central metadata after an odd byte prefix.
    /// </summary>
    [Fact]
    public void Parse_OddOffsetCentral_ReadsMetadata()
    {
        var metadata = BasicFileInfoText.Parse(Bfi(5,
            "Worksharing: Central", "Username: ", @"Central Model Path: C:\c\A.rvt",
            "Format: 2024", "Build: 20230308_1635(x64)"));

        Assert.Equal(WorksharingState.Central, metadata.Worksharing);
        Assert.Equal(@"C:\c\A.rvt", metadata.CentralModelPath);
        Assert.Null(metadata.Username);
        Assert.Equal(2024, metadata.Version);
    }

    /// <summary>
    /// Contract row 2: reads local metadata and the owning username.
    /// </summary>
    [Fact]
    public void Parse_OddOffsetLocal_ReadsMetadataAndUsername()
    {
        var metadata = BasicFileInfoText.Parse(Bfi(5,
            "Worksharing: Local", "Username: tdbinh", @"Central Model Path: \\srv\p\B.rvt",
            "Format: 2025"));

        Assert.Equal(WorksharingState.Local, metadata.Worksharing);
        Assert.Equal("tdbinh", metadata.Username);
        Assert.Equal(@"\\srv\p\B.rvt", metadata.CentralModelPath);
        Assert.Equal(2025, metadata.Version);
    }

    /// <summary>
    /// Contract row 3: preserves disabled worksharing and normalizes empty values.
    /// </summary>
    [Fact]
    public void Parse_NotEnabled_ReturnsNullCentralPath()
    {
        var metadata = BasicFileInfoText.Parse(Bfi(5,
            "Worksharing: Not enabled", "Username: ", "Central Model Path: ", "Format: 2023"));

        Assert.Equal(WorksharingState.NotEnabled, metadata.Worksharing);
        Assert.Null(metadata.CentralModelPath);
        Assert.Null(metadata.Username);
        Assert.Equal(2023, metadata.Version);
    }

    /// <summary>
    /// Contract row 4: reads the same central metadata after an even byte prefix.
    /// </summary>
    [Fact]
    public void Parse_EvenOffsetCentral_ReadsMetadata()
    {
        var metadata = BasicFileInfoText.Parse(Bfi(4,
            "Worksharing: Central", "Username: ", @"Central Model Path: C:\c\A.rvt",
            "Format: 2024", "Build: 20230308_1635(x64)"));

        Assert.Equal(WorksharingState.Central, metadata.Worksharing);
        Assert.Equal(@"C:\c\A.rvt", metadata.CentralModelPath);
        Assert.Null(metadata.Username);
        Assert.Equal(2024, metadata.Version);
    }

    /// <summary>
    /// Contract row 5: a missing worksharing label leaves the state unknown.
    /// </summary>
    [Fact]
    public void Parse_MissingWorksharing_ReturnsUnknownAndVersion()
    {
        var metadata = BasicFileInfoText.Parse(Bfi(5, "Username: x", "Format: 2024"));

        Assert.Equal(WorksharingState.Unknown, metadata.Worksharing);
        Assert.Equal(2024, metadata.Version);
    }

    /// <summary>
    /// Contract row 6: empty input returns unknown metadata without throwing.
    /// </summary>
    [Fact]
    public void Parse_EmptyBytes_ReturnsUnknownMetadata()
    {
        var metadata = BasicFileInfoText.Parse(new byte[0]);

        Assert.Equal(WorksharingState.Unknown, metadata.Worksharing);
        Assert.Null(metadata.Version);
        Assert.Null(metadata.CentralModelPath);
    }

    /// <summary>
    /// Contract row 7: central file details include the worksharing state and path.
    /// </summary>
    [Fact]
    public void Formatter_Central_IncludesFileAndWorksharingDetails()
    {
        var info = CentralInfo();

        var text = RevitVersionInfoFormatter.Build(new[] { info });

        Assert.Contains("Name: A.rvt", text);
        Assert.Contains(@"Path: C:\m\A.rvt", text);
        Assert.Contains("Version: Revit 2024", text);
        Assert.Contains("Worksharing: Central", text);
        Assert.Contains(@"Central model: \\srv\A.rvt", text);
    }

    /// <summary>
    /// Contract row 8: disabled worksharing omits the central model line.
    /// </summary>
    [Fact]
    public void Formatter_NotEnabled_OmitsCentralModelLine()
    {
        var info = NotEnabledInfo();

        var text = RevitVersionInfoFormatter.Build(new[] { info });

        Assert.Contains("Worksharing: Not enabled", text);
        Assert.DoesNotContain("Central model:", text);
    }

    /// <summary>
    /// Contract row 9: multiple file blocks preserve their input order.
    /// </summary>
    [Fact]
    public void Formatter_MultipleFiles_PreservesBlockOrder()
    {
        var text = RevitVersionInfoFormatter.Build(new[] { CentralInfo(), NotEnabledInfo() });

        Assert.Contains("Name: A.rvt", text);
        Assert.Contains("Name: B.rvt", text);
        Assert.True(text.IndexOf("Name: A.rvt", StringComparison.Ordinal)
            < text.IndexOf("Name: B.rvt", StringComparison.Ordinal));
    }

    /// <summary>
    /// Contract row 10: an existing supported file carries detected worksharing.
    /// </summary>
    [Fact]
    public void Inspector_ExistingRvt_CarriesVersionAndCentralWorksharing()
    {
        WithTempRvt(path =>
        {
            var inspector = new RevitFileInspector(new FakeVersionDetector(2024),
                new FakeWorksharingDetector(new WorksharingInfo(WorksharingState.Central, @"\\srv\A.rvt", null)));

            var info = inspector.Inspect(path);

            Assert.Equal(2024, info.Version);
            Assert.Equal(WorksharingState.Central, info.Worksharing.State);
            Assert.True(info.IsWorkshared);
        });
    }

    /// <summary>
    /// Contract row 11: a missing file never invokes the worksharing detector.
    /// </summary>
    [Fact]
    public void Inspector_MissingRvt_SkipsWorksharingDetector()
    {
        const string path = @"C:\does\not\exist.rvt";
        Assert.False(File.Exists(path));
        var detector = new FakeWorksharingDetector(
            new WorksharingInfo(WorksharingState.Central, @"\\srv\A.rvt", null));
        var inspector = new RevitFileInspector(new FakeVersionDetector(2024), detector);

        var info = inspector.Inspect(path);

        Assert.Equal(0, detector.Calls);
        Assert.Equal(WorksharingState.Unknown, info.Worksharing.State);
        Assert.False(info.IsWorkshared);
    }

    /// <summary>
    /// Contract row 12: the original constructor supplies unknown worksharing.
    /// </summary>
    [Fact]
    public void Inspector_OriginalConstructor_DefaultsToUnknownWorksharing()
    {
        WithTempRvt(path =>
        {
            var inspector = new RevitFileInspector(new FakeVersionDetector(2024));

            var info = inspector.Inspect(path);

            Assert.Equal(WorksharingState.Unknown, info.Worksharing.State);
        });
    }

    private static byte[] Bfi(int offset, params string[] lines)
    {
        var prefix = new byte[] { 0x0E, 0, 0, 0, 0 };
        var text = Encoding.Unicode.GetBytes(string.Join("\r\n", lines) + "\r\n");
        var bytes = new byte[offset + text.Length];
        Array.Copy(prefix, bytes, Math.Min(offset, prefix.Length));
        Array.Copy(text, 0, bytes, offset, text.Length);
        return bytes;
    }

    private static RevitInfo CentralInfo()
    {
        return new RevitInfo(@"C:\m\A.rvt", true, true, 2024,
            new WorksharingInfo(WorksharingState.Central, @"\\srv\A.rvt", null));
    }

    private static RevitInfo NotEnabledInfo()
    {
        return new RevitInfo(@"C:\m\B.rvt", true, true, 2023,
            new WorksharingInfo(WorksharingState.NotEnabled, null, null));
    }

    private static void WithTempRvt(Action<string> test)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".rvt");
        try
        {
            File.WriteAllBytes(path, new byte[0]);
            test(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class FakeVersionDetector : IRevitVersionDetector
    {
        private readonly int? _version;

        /// <summary>
        /// Initializes a detector that returns a fixed version.
        /// </summary>
        /// <param name="version">The version to return.</param>
        public FakeVersionDetector(int? version)
        {
            _version = version;
        }

        /// <inheritdoc />
        public int? DetectVersion(string path) => _version;
    }

    private sealed class FakeWorksharingDetector : IRevitWorksharingDetector
    {
        private readonly WorksharingInfo _worksharing;

        /// <summary>
        /// Initializes a detector that returns fixed worksharing information.
        /// </summary>
        /// <param name="worksharing">The information to return.</param>
        public FakeWorksharingDetector(WorksharingInfo worksharing)
        {
            _worksharing = worksharing;
        }

        /// <summary>
        /// Gets the number of detection calls.
        /// </summary>
        public int Calls { get; private set; }

        /// <inheritdoc />
        public WorksharingInfo Detect(string path)
        {
            Calls++;
            return _worksharing;
        }
    }
}
