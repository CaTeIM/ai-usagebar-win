using System;
using System.IO;
using Tomlyn;
using Tomlyn.Model;

namespace AiUsageBar.Services;

/// <summary>
/// Settings for the tray app, deliberately split across two files.
///
/// <para><c>[ui] primary</c> belongs to the Rust CLI: the CLI reads it to pick
/// the vendor to query when no <c>--vendor</c> is passed, so it has to live in
/// the CLI's own config.toml.</para>
///
/// <para><c>poll_seconds</c> is ours alone and is written to a separate file.
/// The CLI rejects unknown top-level keys, so writing it into the CLI's config
/// made every `ai-usagebar` call fail with "unknown field `poll_seconds`",
/// which took the entire app down with a System Error.</para>
/// </summary>
public sealed class Config
{
    private const string PollKey = "poll_seconds";

    public long? PollSeconds { get; set; }
    public UiConfig Ui { get; set; } = new();

    public static Config Load()
    {
        var cfg = new Config();

        var cli = ReadTable(DefaultPath());
        if (cli != null)
        {
            if (cli.TryGetValue("ui", out var uiObj) && uiObj is TomlTable ui
                && ui.TryGetValue("primary", out var primary))
            {
                cfg.Ui.Primary = primary as string;
            }

            // Older builds wrote poll_seconds into the CLI's file. Read it so an
            // existing preference survives the move; Save() strips it from there.
            if (cli.TryGetValue(PollKey, out var legacy) && legacy is long legacySeconds)
            {
                cfg.PollSeconds = legacySeconds;
            }
        }

        // Our own file wins over the legacy location.
        var own = ReadTable(AppConfigPath());
        if (own != null && own.TryGetValue(PollKey, out var poll) && poll is long seconds)
        {
            cfg.PollSeconds = seconds;
        }

        return cfg;
    }

    public TimeSpan PollInterval() => TimeSpan.FromSeconds(Math.Max(PollSeconds ?? 60, 15));

    public void Save()
    {
        SaveOwnSettings();
        SaveCliPrimary();
    }

    private void SaveOwnSettings()
    {
        var path = AppConfigPath() ?? throw new InvalidOperationException("could not resolve the app settings directory");
        var table = ReadTable(path) ?? new TomlTable();
        table[PollKey] = Math.Max(PollSeconds ?? 60, 15);
        Write(path, table);
    }

    /// <summary>Updates <c>[ui] primary</c> in the CLI's config while preserving
    /// every other key, since the CLI keeps credentials in the same file. Also
    /// drops the legacy <c>poll_seconds</c>, repairing a config that the older
    /// behaviour had made unparseable for the CLI.</summary>
    private void SaveCliPrimary()
    {
        var path = DefaultPath() ?? throw new InvalidOperationException("could not resolve config directory");
        var table = ReadTable(path) ?? new TomlTable();

        table.Remove(PollKey);

        if (!string.IsNullOrEmpty(Ui.Primary))
        {
            if (!table.TryGetValue("ui", out var uiObj) || uiObj is not TomlTable uiTable)
            {
                uiTable = new TomlTable();
                table["ui"] = uiTable;
            }
            uiTable["primary"] = Ui.Primary;
        }

        Write(path, table);
    }

    private static TomlTable? ReadTable(string? path)
    {
        if (path == null || !File.Exists(path)) return null;
        try
        {
            return Toml.ToModel(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }

    private static void Write(string path, TomlTable table)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, Toml.FromModel(table));
    }

    /// <summary>The Rust CLI's config file. Owned by the CLI; we touch only
    /// <c>[ui] primary</c>. This is what the "Open config.toml" button opens.</summary>
    public static string? DefaultPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return string.IsNullOrEmpty(appData) ? null : Path.Combine(appData, "ai-usagebar", "config", "config.toml");
    }

    /// <summary>This app's own settings file. The CLI never reads it, so keys
    /// here can never break the CLI's parser.</summary>
    public static string? AppConfigPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return string.IsNullOrEmpty(appData) ? null : Path.Combine(appData, "ai-usagebar-win", "settings.toml");
    }

    public string PrimaryStr() => string.IsNullOrEmpty(Ui.Primary) ? "anthropic" : Ui.Primary;
}

public sealed class UiConfig
{
    public string? Primary { get; set; }
}
