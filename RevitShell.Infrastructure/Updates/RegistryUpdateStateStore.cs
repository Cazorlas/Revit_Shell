using System;
using System.Globalization;
using Microsoft.Win32;
using RevitShell.Application;

namespace RevitShell.Infrastructure;

/// <summary>Stores update preferences in the current user's registry.</summary>
public sealed class RegistryUpdateStateStore : IUpdateStateStore
{
    /// <summary>Identifies the default update preferences key.</summary>
    public const string DefaultSubKey = @"Software\PaperEngineer\Shell";
    private readonly string _subKeyPath;

    /// <summary>Initializes the registry path without creating the key.</summary>
    public RegistryUpdateStateStore(string subKeyPath)
    {
        _subKeyPath = subKeyPath ?? throw new ArgumentNullException(nameof(subKeyPath));
    }

    /// <summary>Gets or sets the last feed check as a round-trip UTC timestamp.</summary>
    public DateTime? LastCheckUtc
    {
        get
        {
            var text = ReadString(nameof(LastCheckUtc));
            return DateTime.TryParseExact(text, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time)
                ? time.ToUniversalTime()
                : (DateTime?)null;
        }
        set => WriteString(nameof(LastCheckUtc), value?.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
    }

    /// <summary>Gets or sets the release version skipped by the user.</summary>
    public string? SkippedVersion
    {
        get => ReadString(nameof(SkippedVersion));
        set => WriteString(nameof(SkippedVersion), value);
    }

    private string? ReadString(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(_subKeyPath);
        return key?.GetValue(name) as string;
    }

    private void WriteString(string name, string? value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(_subKeyPath);
        if (value == null)
        {
            key.DeleteValue(name, false);
        }
        else
        {
            key.SetValue(name, value, RegistryValueKind.String);
        }
    }
}
