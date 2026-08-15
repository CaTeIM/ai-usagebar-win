using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using AiUsageBar.Models;
using AiUsageBar.Services;
using Wpf.Ui.Controls;

namespace AiUsageBar.Views;

public partial class SettingsWindow : FluentWindow
{
    public event Action? Saved;

    private UsageJsonRoot _root = new();
    private SettingsModel _model = new();

    public SettingsWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    public void ShowWith(Config cfg, UsageJsonRoot root)
    {
        _root = root;
        Populate(cfg);
        Show();
        Activate();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    private void Populate(Config cfg)
    {
        _model = Renderer.SettingsModel(cfg, _root);
        PollBox.Value = _model.PollSeconds;
        
        PrimaryBox.Items.Clear();
        foreach (var entry in _root.Entries)
        {
            PrimaryBox.Items.Add(entry.DisplayName);
        }

        // Set selected primary
        var pIndex = _root.Entries.FindIndex(e => e.Id == _model.Primary);
        PrimaryBox.SelectedIndex = pIndex >= 0 ? pIndex : 0;

        StartupBox.IsChecked = StartupService.IsEnabled();
    }

    private void OnOpenConfig(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = Config.DefaultPath();
            if (path != null)
            {
                var dir = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
                if (!System.IO.File.Exists(path)) System.IO.File.WriteAllText(path, "");
                Process.Start("notepad.exe", path);
            }
        }
        catch { }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var cfg = Config.Load();
        
        cfg.PollSeconds = PollBox.Value is double d ? (long)d : 60;
        
        var idx = PrimaryBox.SelectedIndex < 0 ? 0 : PrimaryBox.SelectedIndex;
        if (idx < _root.Entries.Count)
        {
            cfg.Ui.Primary = _root.Entries[idx].Id;
        }

        var sane = cfg.Sanitized();
        try
        {
            sane.Save();
        }
        catch { }

        StartupService.SetEnabled(StartupBox.IsChecked == true);

        Saved?.Invoke();
        Hide();
    }

    private void OnClose(object sender, RoutedEventArgs e) => Hide();
}
