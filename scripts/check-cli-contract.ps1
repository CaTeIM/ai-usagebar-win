<#
.SYNOPSIS
    Checks that the installed `ai-usagebar` CLI still matches what this app expects.

.DESCRIPTION
    This app is a wrapper: it parses the CLI's JSON and renders it. When the CLI
    changes its schema, most of the damage is silent, because System.Text.Json
    ignores unknown fields and defaults missing ones. A renamed field shows up as
    a vendor with no bars, not as an error.

    Run this after every `cargo install ai-usagebar --force` to catch that early.

    Errors mean the app will misbehave. Notices mean the CLI grew something the
    app does not consume yet, which is informational, not a failure.

.EXAMPLE
    pwsh ./scripts/check-cli-contract.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# Mirrors AiUsageBar/Models/Interop.cs. Keep both sides in sync.
$RootFields    = @('entries', 'primary')
$EntryFields   = @('display_name', 'error', 'fetched_at', 'id', 'metrics',
                   'name', 'plan', 'sections', 'stale', 'status')
$MetricFields  = @('detail', 'label', 'percent', 'reset_at', 'severity', 'value')
$SectionFields = @('type', 'detail', 'label', 'percent', 'reset_at', 'severity',
                   'value', 'text')

# Mirrors SeverityRules.Parse and the section handling in Renderer.cs.
$KnownSeverities  = @('low', 'mid', 'high', 'critical')
$KnownSectionType = @('metric', 'fact', 'spacer')
$ExpectedSettingsSchema = 1

$contractErrors  = [System.Collections.Generic.List[string]]::new()
$contractNotices = [System.Collections.Generic.List[string]]::new()

function Get-Fields($obj) {
    if ($null -eq $obj) { return @() }
    return $obj.PSObject.Properties.Name
}

function Compare-Shape($obj, [string[]]$expected, [string]$label) {
    $actual  = Get-Fields $obj
    $missing = $expected | Where-Object { $_ -notin $actual }
    $extra   = $actual   | Where-Object { $_ -notin $expected }
    if ($missing) { $script:contractErrors.Add("$label is missing: $($missing -join ', ')") }
    if ($extra)   { $script:contractNotices.Add("$label has new fields the app ignores: $($extra -join ', ')") }
}

# -- CLI presence and version ------------------------------------------------

$cli = Get-Command ai-usagebar -ErrorAction SilentlyContinue
if (-not $cli) {
    Write-Host "ai-usagebar not found in PATH. Install it with: cargo install ai-usagebar" -ForegroundColor Red
    exit 1
}

$version = (& ai-usagebar --version 2>&1 | Select-Object -First 1)
Write-Host "CLI:  $version"
Write-Host "Path: $($cli.Source)"
Write-Host ""

# -- usage --json ------------------------------------------------------------

$raw = & ai-usagebar usage --json 2>&1
$usageExit = $LASTEXITCODE

# Parse first, judge the exit code second. A machine with no credentials at all
# (a CI runner, for instance) can exit non-zero while still emitting a perfectly
# well-formed document, and the shape is what this script is here to check. An
# unparseable payload is the real failure: that is what a rejected config.toml or
# a changed output mode looks like.
try {
    $usage = $raw | ConvertFrom-Json
} catch {
    Write-Host "'ai-usagebar usage --json' did not return valid JSON (exit $usageExit)." -ForegroundColor Red
    Write-Host $raw
    exit 1
}

if ($usageExit -ne 0) {
    $contractNotices.Add("'ai-usagebar usage --json' exited with $usageExit but returned valid JSON (expected when no vendor is configured).")
}

Compare-Shape $usage $RootFields 'usage root'

$entries = @($usage.entries)
Write-Host "Entries: $($entries.Count)"

foreach ($entry in $entries) {
    $id = if ($entry.id) { $entry.id } else { '<no id>' }
    Compare-Shape $entry $EntryFields "entry '$id'"

    foreach ($metric in @($entry.metrics)) {
        Compare-Shape $metric $MetricFields "entry '$id' metric"
        if ($metric.severity -and $metric.severity -notin $KnownSeverities) {
            $contractErrors.Add("entry '$id' has unknown severity '$($metric.severity)'. Renderer.cs will treat it as Unknown (grey).")
        }
    }

    foreach ($section in @($entry.sections)) {
        if ($section.type -notin $KnownSectionType) {
            $contractNotices.Add("entry '$id' has section type '$($section.type)', which the app does not render.")
        }
        # Only metric sections feed the popup bars, so only they need the full shape.
        if ($section.type -eq 'metric') {
            $expected = $SectionFields | Where-Object { $_ -ne 'text' }
            Compare-Shape $section $expected "entry '$id' metric section"
        }
    }
}

# -- settings show -----------------------------------------------------------

$settingsRaw = & ai-usagebar settings show 2>&1
if ($LASTEXITCODE -ne 0) {
    $contractNotices.Add("'ai-usagebar settings show' is unavailable in this version.")
} else {
    try {
        $settings = $settingsRaw | ConvertFrom-Json
        if ($settings.schema_version -ne $ExpectedSettingsSchema) {
            $contractErrors.Add("settings schema_version is $($settings.schema_version), expected $ExpectedSettingsSchema.")
        }
    } catch {
        $contractErrors.Add("'ai-usagebar settings show' did not return valid JSON.")
    }
}

# -- Report ------------------------------------------------------------------

Write-Host ""
foreach ($n in $contractNotices) { Write-Host "NOTICE  $n" -ForegroundColor Yellow }
foreach ($e in $contractErrors)  { Write-Host "ERROR   $e" -ForegroundColor Red }

Write-Host ""
if ($contractErrors.Count -gt 0) {
    Write-Host "Contract check FAILED with $($contractErrors.Count) error(s)." -ForegroundColor Red
    Write-Host "Update AiUsageBar/Models/Interop.cs and Renderer.cs, then re-run."
    exit 1
}

Write-Host "Contract OK." -ForegroundColor Green
if ($contractNotices.Count -gt 0) {
    Write-Host "$($contractNotices.Count) notice(s) above are informational."
}
exit 0
