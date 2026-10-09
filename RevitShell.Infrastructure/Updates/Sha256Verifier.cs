using System;
using System.IO;
using System.Security.Cryptography;

namespace RevitShell.Infrastructure;

/// <summary>Computes and verifies file SHA-256 digests.</summary>
public static class Sha256Verifier
{
    /// <summary>Computes the lowercase hexadecimal digest of a file's bytes.</summary>
    public static string Compute(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha256 = SHA256.Create();
        return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
    }

    /// <summary>Checks a file against a supplied digest without case sensitivity.</summary>
    public static bool Matches(string path, string? expectedHex)
    {
        return expectedHex != null && string.Equals(Compute(path), expectedHex, StringComparison.OrdinalIgnoreCase);
    }
}
