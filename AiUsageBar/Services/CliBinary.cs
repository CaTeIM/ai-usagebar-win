using System;
using System.IO;
using System.Reflection;

namespace AiUsageBar.Services;

/// <summary>
/// Decides which <c>ai-usagebar</c> executable to run.
///
/// <para>Release builds embed the Rust CLI as a resource (see the conditional
/// <c>EmbeddedResource</c> in the .csproj), so a fresh install works without
/// <c>cargo install</c>. It is extracted once to LocalAppData and re-extracted
/// whenever the app version changes.</para>
///
/// <para>The bundled copy deliberately wins over anything on PATH: everyone then
/// runs the exact CLI build this release was tested against, instead of whatever
/// version each machine happens to have.</para>
///
/// <para>Local builds carry no resource and fall back to PATH, so development
/// keeps working against a locally installed CLI.</para>
/// </summary>
public static class CliBinary
{
    private const string ResourceName = "ai-usagebar.exe";

    /// <summary>Used when nothing is bundled: resolved through PATH by Windows.</summary>
    private const string FallbackCommand = "ai-usagebar";

    private static string? _resolved;

    /// <summary>What to hand to <c>ProcessStartInfo.FileName</c>. Resolved once
    /// per process, since neither the resource nor PATH changes while running.</summary>
    public static string Executable => _resolved ??= Resolve();

    private static string Resolve()
    {
        try
        {
            return EnsureExtracted() ?? FallbackCommand;
        }
        catch (Exception)
        {
            // Extraction is best-effort. A locked file, a full disk or a locked
            // down profile should degrade to "use whatever is on PATH", not take
            // the app down. Poller reports the failure if PATH has nothing either.
            return FallbackCommand;
        }
    }

    /// <summary>Returns the extracted path, or null when no CLI is bundled.</summary>
    private static string? EnsureExtracted()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var resource = assembly.GetManifestResourceStream(ResourceName);
        if (resource == null) return null;

        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ai-usagebar-win",
            "bin");
        Directory.CreateDirectory(dir);

        var exePath = Path.Combine(dir, ResourceName);
        var stampPath = exePath + ".version";
        var version = assembly.GetName().Version?.ToString() ?? "unknown";

        if (File.Exists(exePath) && ReadStamp(stampPath) == version) return exePath;

        // Write to a temp file and move it into place, so an interrupted run
        // never leaves a half-written executable that would fail cryptically.
        var tempPath = exePath + ".tmp";
        using (var file = File.Create(tempPath))
        {
            resource.CopyTo(file);
        }

        File.Move(tempPath, exePath, overwrite: true);
        File.WriteAllText(stampPath, version);

        return exePath;
    }

    private static string? ReadStamp(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
