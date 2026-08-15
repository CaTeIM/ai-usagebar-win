using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiUsageBar.Models;

public sealed class UsageJsonRoot
{
    [JsonPropertyName("entries")]
    public List<UsageJsonEntry> Entries { get; set; } = new();

    [JsonPropertyName("primary")]
    public string? Primary { get; set; }
}

public sealed class UsageJsonEntry
{
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = "";

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("fetched_at")]
    public DateTimeOffset? FetchedAt { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("metrics")]
    public List<UsageJsonMetric> Metrics { get; set; } = new();

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("plan")]
    public string? Plan { get; set; }

    [JsonPropertyName("sections")]
    public List<UsageJsonSection> Sections { get; set; } = new();

    [JsonPropertyName("stale")]
    public bool Stale { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";
}

public sealed class UsageJsonMetric
{
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("percent")]
    public int Percent { get; set; }

    [JsonPropertyName("reset_at")]
    public DateTimeOffset? ResetAt { get; set; }

    [JsonPropertyName("severity")]
    public string? Severity { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; } = "";
}

public sealed class UsageJsonSection
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    // For "metric" type:
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("percent")]
    public int? Percent { get; set; }

    [JsonPropertyName("reset_at")]
    public DateTimeOffset? ResetAt { get; set; }

    [JsonPropertyName("severity")]
    public string? Severity { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    // For "fact" type:
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
