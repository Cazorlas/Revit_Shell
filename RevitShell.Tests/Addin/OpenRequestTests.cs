using System;
using System.IO;
using System.Text;
using RevitShell.RevitAddin.Requests;
using Xunit;

namespace RevitShell.Tests.Addin;

/// <summary>
/// Verifies the request file and local model naming contracts without Revit.
/// </summary>
public sealed class OpenRequestTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "AddinRequests-" + Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Creates an isolated directory for request files.
    /// </summary>
    public OpenRequestTests()
    {
        Directory.CreateDirectory(_directory);
    }

    /// <summary>
    /// Reads a detach request with LF line endings.
    /// </summary>
    [Fact]
    public void TryRead_DetachRequest_ReturnsModeAndModelPath()
    {
        var path = WriteRequest("RevitShellOpenRequest=1\nMode=Detach\nModelPath=C:\\m\\A.rvt");

        Assert.True(OpenRequestFile.TryRead(path, out var request));
        Assert.NotNull(request);
        Assert.Equal(OpenRequestMode.Detach, request!.Mode);
        Assert.Equal(@"C:\m\A.rvt", request.ModelPath);
    }

    /// <summary>
    /// Accepts a UTF-8 BOM, CRLF line endings and case-insensitive keys and modes.
    /// </summary>
    [Fact]
    public void TryRead_Utf8BomAndLowercaseCreateLocal_ReturnsCreateLocal()
    {
        var path = WriteRequest("\uFEFFRevitShellOpenRequest=1\r\nmode=createlocal\r\nModelPath=C:\\m\\A.rvt\r\n");

        Assert.True(OpenRequestFile.TryRead(path, out var request));
        Assert.NotNull(request);
        Assert.Equal(OpenRequestMode.CreateLocal, request!.Mode);
    }

    /// <summary>
    /// Rejects a request without the required header.
    /// </summary>
    [Fact]
    public void TryRead_MissingHeader_ReturnsFalseAndNull()
    {
        var path = WriteRequest("Mode=Open\nModelPath=C:\\m\\A.rvt");

        Assert.False(OpenRequestFile.TryRead(path, out var request));
        Assert.Null(request);
    }

    /// <summary>
    /// Rejects an unknown mode.
    /// </summary>
    [Fact]
    public void TryRead_UnknownMode_ReturnsFalse()
    {
        var path = WriteRequest("RevitShellOpenRequest=1\nMode=Bogus\nModelPath=C:\\m\\A.rvt");

        Assert.False(OpenRequestFile.TryRead(path, out _));
    }

    /// <summary>
    /// Preserves equals signs after the first key-value separator.
    /// </summary>
    [Fact]
    public void TryRead_ModelPathContainingEquals_PreservesModelPath()
    {
        var path = WriteRequest("RevitShellOpenRequest=1\nMode=Open\nModelPath=C:\\a=b\\A.rvt");

        Assert.True(OpenRequestFile.TryRead(path, out var request));
        Assert.NotNull(request);
        Assert.Equal(@"C:\a=b\A.rvt", request!.ModelPath);
    }

    /// <summary>
    /// Returns failure without throwing when the request file does not exist.
    /// </summary>
    [Fact]
    public void TryRead_MissingFile_ReturnsFalseWithoutThrowing()
    {
        var path = Path.Combine(_directory, "missing.txt");

        Assert.False(OpenRequestFile.TryRead(path, out _));
    }

    /// <summary>
    /// Consumes a valid request by deleting its file.
    /// </summary>
    [Fact]
    public void TryRead_ValidRequest_DeletesRequestFile()
    {
        var path = WriteRequest("RevitShellOpenRequest=1\nMode=Detach\nModelPath=C:\\m\\A.rvt");
        Assert.True(File.Exists(path));

        Assert.True(OpenRequestFile.TryRead(path, out _));

        Assert.False(File.Exists(path));
    }

    /// <summary>
    /// Rejects an empty model path.
    /// </summary>
    [Fact]
    public void TryRead_EmptyModelPath_ReturnsFalse()
    {
        var path = WriteRequest("RevitShellOpenRequest=1\nMode=Open\nModelPath=");

        Assert.False(OpenRequestFile.TryRead(path, out _));
    }

    /// <summary>
    /// Names a local copy from a UNC central model and Revit username.
    /// </summary>
    [Fact]
    public void GetLocalPath_UncModel_ReturnsUserLocalInDocuments()
    {
        var path = LocalModelNaming.GetLocalPath(@"\\srv\p\Tower.rvt", "tdbinh", @"C:\Users\u\Documents");

        Assert.Equal(@"C:\Users\u\Documents\Tower_tdbinh.rvt", path);
    }

    /// <summary>
    /// Replaces invalid filename characters in the username with underscores.
    /// </summary>
    [Fact]
    public void GetLocalPath_InvalidUsernameCharacters_ReplacesEachWithUnderscore()
    {
        var path = LocalModelNaming.GetLocalPath(@"\\srv\p\Tower.rvt", "a/b:c", @"C:\Users\u\Documents");

        Assert.Equal(@"C:\Users\u\Documents\Tower_a_b_c.rvt", path);
    }

    /// <summary>
    /// Names a backup with a timestamp before the RVT extension.
    /// </summary>
    [Fact]
    public void GetBackupPath_ExistingLocal_ReturnsTimestampedBackup()
    {
        var path = LocalModelNaming.GetBackupPath(@"C:\d\Tower_x.rvt", new DateTime(2026, 10, 9, 13, 5, 7));

        Assert.Equal(@"C:\d\Tower_x_backup_20261009-130507.rvt", path);
    }

    /// <summary>
    /// Removes the request files created by this test instance.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    private string WriteRequest(string content)
    {
        var path = Path.Combine(_directory, "request.txt");
        File.WriteAllText(path, content, new UTF8Encoding(false));
        return path;
    }
}
