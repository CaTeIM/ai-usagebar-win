using System;
using System.IO;
using Tomlyn;
using Tomlyn.Model;

namespace AiUsageBar.Services;

/// <summary>
/// Minimal config for the system tray settings (poll_seconds, UI primary, start with windows).
/// Provider configurations are delegated to the underlying ai-usagebar Rust CLI.
/// </summary>
public sealed class Config
{
    public long? PollSeconds { get; set; }
    public UiConfig Ui { get; set; } = new();

    public static Config Load()
    {
        var path = DefaultPath();
        if (path == null || !File.Exists(path)) return new Config();
        try
        {
            var text = File.ReadAllText(path);
            var options = new TomlModelOptions { IgnoreMissingProperties = true };
            return Toml.ToModel<Config>(text, options: options);
        }
        catch
        {
            return new Config();
        }
    }

    public TimeSpan PollInterval() => TimeSpan.FromSeconds(Math.Max(PollSeconds ?? 60, 15));

    public void Save()
    {
        var path = DefaultPath() ?? throw new InvalidOperationException("could not resolve config directory");
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        TomlTable table;
        if (File.Exists(path))
        {
            try
            {
                var text = File.ReadAllText(path);
                table = Toml.ToModel(text);
            }
            catch
            {
                table = new TomlTable();
            }
        }
        else
        {
            table = new TomlTable();
        }

        // Update only the properties we manage
        table["poll_seconds"] = Math.Max(PollSeconds ?? 60, 15);

        if (!string.IsNullOrEmpty(Ui.Primary))
        {
            if (!table.TryGetValue("ui", out var uiObj) || uiObj is not TomlTable uiTable)
            {
                uiTable = new TomlTable();
                table["ui"] = uiTable;
            }
            uiTable["primary"] = Ui.Primary;
        }

        File.WriteAllText(path, Toml.FromModel(table));
    }

    public static string? DefaultPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return string.IsNullOrEmpty(appData) ? null : Path.Combine(appData, "ai-usagebar", "config", "config.toml");
    }

    public string PrimaryStr() => string.IsNullOrEmpty(Ui.Primary) ? "anthropic" : Ui.Primary;
}

public sealed class UiConfig
{
    public string? Primary { get; set; }
}
