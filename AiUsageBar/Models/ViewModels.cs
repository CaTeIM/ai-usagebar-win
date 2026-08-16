using System.Collections.Generic;

namespace AiUsageBar.Models;

// ---------------------------------------------------------------------------
// Popup view-model: only vendors with an identified key (Ok, or configured
// but currently erroring). Login-needed / unconfigured vendors are hidden here
// and surfaced in the settings window instead.
// ---------------------------------------------------------------------------

public sealed class PopupModel
{
    public List<VendorCard> Vendors { get; init; } = new();
    public bool IsEmpty => Vendors.Count == 0;
}

public sealed class VendorCard
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Plan { get; init; }
    /// <summary>"ok" | "error"</summary>
    public string Status { get; init; } = "ok";
    public string? Message { get; init; }
    public List<Bar> Bars { get; init; } = new();
    public List<Fact> Facts { get; init; } = new();

    public bool HasPlan => !string.IsNullOrEmpty(Plan);
    public bool HasMessage => !string.IsNullOrEmpty(Message);
}

public sealed class Bar
{
    public string Label { get; init; } = "";
    public int Pct { get; init; }
    public string? Reset { get; init; }
    /// <summary>"low" | "mid" | "high" | "critical"</summary>
    public string Level { get; init; } = "low";

    public string ValueText => string.IsNullOrEmpty(Reset) ? $"{Pct}%" : $"{Pct}%  ·  {Reset}";
}

public sealed class Fact
{
    public string Label { get; init; } = "";
    public string Value { get; init; } = "";
    public string Text => $"{Label}: {Value}";
}

// ---------------------------------------------------------------------------
// Settings view-model: every supported vendor, configured or not. Editable
// fields are mutated in place by the settings form's two-way bindings.
// ---------------------------------------------------------------------------

public sealed class SettingsModel
{
    public long PollSeconds { get; set; }
    public string Primary { get; set; } = "anthropic";
    public List<VendorSetting> Vendors { get; init; } = new();
}

public sealed class VendorSetting
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Status { get; init; }

    public bool HasStatus => !string.IsNullOrEmpty(Status);
}
