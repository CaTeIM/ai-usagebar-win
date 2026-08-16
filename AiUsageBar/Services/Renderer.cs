using System;
using System.Collections.Generic;
using System.Linq;
using AiUsageBar.Models;

namespace AiUsageBar.Services;

public static class Renderer
{
    public sealed record Rendered(Severity Severity, string Tooltip);

    public static Rendered Render(UsageJsonRoot root, Config cfg, DateTimeOffset now)
    {
        var worstSeverity = Severity.Unknown;
        var tipLines = new List<string>();

        foreach (var entry in Ordered(root.Entries, root.Primary))
        {
            if (!ShouldShow(entry, cfg)) continue;

            // Compute worst severity
            var entrySeverity = GetWorstSeverity(entry);
            if (entrySeverity > worstSeverity || worstSeverity == Severity.Unknown)
            {
                worstSeverity = entrySeverity;
            }

            // Build tooltip line
            var tag = entry.Id.Substring(0, Math.Min(3, entry.Id.Length));
            if (entry.Id == "anthropic") tag = "cld";
            else if (entry.Id == "openai") tag = "gpt";
            else if (entry.Id == "openrouter") tag = "or";
            else if (entry.Id == "deepseek") tag = "ds";
            else if (entry.Id == "antigravity") tag = "agy";
            else if (entry.Id == "moonshot") tag = "moo";
            else if (entry.Id == "supergrok") tag = "sgk";

            if (entry.Status != "ready")
            {
                tipLines.Add($"{tag}: {entry.Status}");
                continue;
            }

            // Find the most critical metric
            var worstMetric = entry.Metrics.OrderByDescending(m => SeverityRules.Parse(m.Severity ?? "")).FirstOrDefault();
            if (worstMetric != null)
            {
                tipLines.Add($"{tag} {worstMetric.Value} · {worstMetric.Label}");
            }
            else
            {
                // Fallback for providers with facts/balance only
                var fact = entry.Sections.FirstOrDefault(s => s.Type == "fact");
                if (fact != null && !string.IsNullOrEmpty(fact.Text))
                {
                    tipLines.Add($"{tag} {fact.Text}");
                }
                else
                {
                    tipLines.Add($"{tag} ready");
                }
            }
        }

        var tooltip = tipLines.Count == 0
            ? "ai-usagebar - no models configured"
            : string.Join("\n", tipLines);

        return new Rendered(worstSeverity, tooltip);
    }

    private static IEnumerable<UsageJsonEntry> Ordered(List<UsageJsonEntry> entries, string? primaryId)
        => entries.OrderBy(r => r.Id != primaryId).ThenBy(r => r.Id);

    private static bool ShouldShow(UsageJsonEntry entry, Config cfg)
    {
        if (entry.Status == "ready") return true;
        // If it's configured in cfg but erroring, show it
        return cfg.IsConfiguredId(entry.Id);
    }

    private static Severity GetWorstSeverity(UsageJsonEntry entry)
    {
        if (entry.Status != "ready") return Severity.Critical;
        if (entry.Metrics == null || entry.Metrics.Count == 0) return Severity.Low;
        var severities = entry.Metrics.Select(m => SeverityRules.Parse(m.Severity ?? "low")).ToList();
        return severities.Max();
    }

    // -- Popup ---------------------------------------------------------------

    public static PopupModel PopupModel(UsageJsonRoot root, Config cfg, DateTimeOffset now)
    {
        var model = new PopupModel();
        foreach (var entry in Ordered(root.Entries, root.Primary))
        {
            if (!ShouldShow(entry, cfg)) continue;

            if (entry.Status == "ready")
            {
                model.Vendors.Add(OkCard(entry));
            }
            else
            {
                model.Vendors.Add(new VendorCard
                {
                    Id = entry.Id,
                    Name = entry.DisplayName,
                    Status = "error",
                    Message = entry.Error ?? entry.Status,
                });
            }
        }
        return model;
    }

    private static VendorCard OkCard(UsageJsonEntry entry)
    {
        var bars = new List<Bar>();
        var facts = new List<Fact>();

        foreach (var section in entry.Sections)
        {
            if (section.Type == "metric")
            {
                bars.Add(new Bar
                {
                    Label = section.Label ?? "",
                    Pct = section.Percent ?? 0,
                    Level = section.Severity ?? "low",
                    Reset = section.Detail // We'll put detail in Reset since the UI binds to it as a secondary string
                });
            }
            else if (section.Type == "fact" && !string.IsNullOrEmpty(section.Text))
            {
                var parts = section.Text.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    facts.Add(new Fact { Label = parts[0].Trim(), Value = parts[1].Trim() });
                }
                else
                {
                    facts.Add(new Fact { Label = "Info", Value = section.Text });
                }
            }
        }

        // Add a general message if the entry has an error but status is ready (e.g. stale warning)
        string? message = null;
        if (entry.Stale) message = "Data is stale (offline).";
        if (!string.IsNullOrEmpty(entry.Error)) message = entry.Error;

        return new VendorCard
        {
            Id = entry.Id,
            Name = entry.DisplayName,
            Plan = entry.Plan,
            Status = "ok",
            Message = message,
            Bars = bars,
            Facts = facts,
        };
    }

    // -- Settings ------------------------------------------------------------

    public static SettingsModel SettingsModel(Config cfg, UsageJsonRoot root)
    {
        var model = new SettingsModel
        {
            PollSeconds = Math.Max(cfg.PollSeconds ?? 60, 15),
            Primary = cfg.PrimaryStr(),
        };

        foreach (var entry in root.Entries)
        {
            model.Vendors.Add(VendorSetting(entry, cfg));
        }

        return model;
    }

    private static VendorSetting VendorSetting(UsageJsonEntry entry, Config cfg)
    {
        var status = entry.Status == "ready" ? "Connected" : $"Error - {entry.Error ?? entry.Status}";

        return new VendorSetting
        {
            Id = entry.Id,
            Name = entry.DisplayName,
            Status = status,
        };
    }
}

public enum Severity { Unknown, Low, Mid, High, Critical }

public static class SeverityRules
{
    public static Severity Parse(string level) => level switch
    {
        "critical" => Severity.Critical,
        "high" => Severity.High,
        "mid" => Severity.Mid,
        _ => Severity.Low,
    };

    public static Severity ForPct(int pct) => pct switch
    {
        >= 90 => Severity.Critical,
        >= 75 => Severity.High,
        >= 50 => Severity.Mid,
        _ => Severity.Low,
    };
}
