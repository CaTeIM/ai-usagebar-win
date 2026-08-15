using System;
using Microsoft.Win32;

namespace AiUsageBar.Services;

/// <summary>Manages "start with Windows" for this unpackaged app via the per-user
/// Run key (<c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c>). Enabling
/// writes the current executable path; disabling removes the value. Per-user, so
/// it needs no administrator rights.</summary>
public static class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "AiUsageBar";

    /// <summary>Path of the running executable. For a single-file published app
    /// this is the bundle .exe (the thing to relaunch), which is what we want.</summary>
    private static string ExePath => Environment.ProcessPath ?? "";

    /// <summary>True if a Run entry for this app exists.</summary>
    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(ValueName) is string s && !string.IsNullOrWhiteSpace(s);
    }

    /// <summary>Add or remove the Run entry. Best-effort: registry access failures
    /// are swallowed so a toggle never crashes the app.</summary>
    public static void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKey);
            if (key is null) return;

            if (enabled)
            {
                var path = ExePath;
                if (!string.IsNullOrEmpty(path))
                    key.SetValue(ValueName, $"\"{path}\""); // quote: path may contain spaces
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Best-effort — never let a startup-toggle failure take down the app.
        }
    }
}
