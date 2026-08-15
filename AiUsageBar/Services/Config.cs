using System;
using System.IO;
using Tomlyn;
using System.Collections.Generic;

namespace AiUsageBar.Services;

/// <summary>
/// Minimal config for the system tray settings (poll_seconds, UI primary, start with windows).
/// Provider configurations are delegated to the underlying ai-usagebar Rust CLI.
/// </summary>
public sealed class Config
{
    public long? PollSeconds { get; set; }
    public bool? RefreshTokens { get; set; }
    public UiConfig Ui { get; set; } = new();

    // Store unknown sections dynamically to preserve them when saving
    public Dictionary<string, object> DynamicTable { get; set; } = new();

    private static readonly TomlModelOptions TomlOptions = new()
    {
        IgnoreMissingProperties = true,
    };

    public static Config Load()
    {
        var path = DefaultPath();
        if (path == null || !File.Exists(path)) return new Config();
        try
        {
            var text = File.ReadAllText(path);
            return Toml.ToModel<Config>(text, options: TomlOptions);
        }
        catch
        {
            return new Config();
        }
    }

    public TimeSpan PollInterval() => TimeSpan.FromSeconds(Math.Max(PollSeconds ?? 60, 15));

    public bool RefreshEnabled() => RefreshTokens == true;

    public Config Sanitized()
    {
        if (PollSeconds is { } p) PollSeconds = Math.Max(p, 15);
        return this;
    }

    public void Save()
    {
        var path = DefaultPath() ?? throw new InvalidOperationException("could not resolve config directory");
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, Toml.FromModel(this, TomlOptions));
    }

    public static string? DefaultPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        // We continue to read from the Rust CLI config file so settings are shared.
        return string.IsNullOrEmpty(appData) ? null : Path.Combine(appData, "ai-usagebar", "config", "config.toml");
    }

    public string PrimaryStr() => string.IsNullOrEmpty(Ui.Primary) ? "anthropic" : Ui.Primary;

    // Helper stubs since we no longer track individual vendors in C#
    public bool IsEnabledId(string id) => true;
    public bool IsConfiguredId(string id) => true; // Always true, we let Rust CLI handle configuration errors
}

public sealed class UiConfig
{
    public string? Primary { get; set; }
}
