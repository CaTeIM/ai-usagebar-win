# CLAUDE.md

Single source of truth for this repository. Read this before changing anything.

## Overview

`ai-usagebar-win` is a Windows system-tray app that shows AI plan usage at a
glance. It is a **thin wrapper** around the Rust CLI
[`ai-usagebar`](https://github.com/akitaonrails/ai-usagebar): the C# side runs
`ai-usagebar usage --json`, parses stdout, and renders it. It never calls
provider APIs and never handles credentials.

The project is a structural evolution of
[FranzoiDev/ai-usagebar-win](https://github.com/FranzoiDev/ai-usagebar-win).
The WPF UI was preserved; the internals were rewritten as the wrapper described
above.

## Tech stack

| Piece | Version / notes |
|---|---|
| .NET | 8 (`net8.0-windows10.0.19041.0`) |
| UI | WPF, styled with WPF-UI 4.3.0 (Fluent, Mica, dark theme) |
| Tray icon | H.NotifyIcon.Wpf 2.2.0 |
| Icon drawing | System.Drawing.Common 9.0.0 |
| Config format | TOML via Tomlyn 0.17.0 |
| Platforms | `x64` and `arm64` (there is no `AnyCPU`) |
| Versioning | CalVer, see below |

## Architecture

```
Poller  ──runs──>  ai-usagebar usage --json  ──stdout──>  UsageJsonRoot
   │                                                          │
   └── raises Updated(Config, UsageJsonRoot) on the UI thread ─┘
                              │
                          Renderer
                              ├─> Rendered(Severity, Tooltip)  ─> TrayService
                              ├─> PopupModel                   ─> PopupWindow
                              └─> SettingsModel                ─> SettingsWindow
```

`Poller` owns all process execution and never throws at the caller: every
failure path (binary missing, non-zero exit, timeout, invalid JSON) is turned
into a synthetic entry with id `UsageJsonEntry.SystemId` and status `error`, so
problems surface in the UI instead of disappearing.

## Directory structure

```
AiUsageBar/
  App.xaml.cs              tray-first wiring, single instance, first-run shortcut
  Converters.cs            XAML value converters (severity to brush, etc.)
  Models/
    Interop.cs             JSON contract for `ai-usagebar usage --json`
    ViewModels.cs          popup and settings view-models bound by XAML
  Services/
    Config.cs              TOML load/save (poll interval, UI primary)
    Poller.cs              background polling loop, runs the Rust CLI
    Renderer.cs            JSON to tooltip / popup / settings models
    TrayIconFactory.cs     severity-tinted tray icon drawn in code
    TrayService.cs         H.NotifyIcon wrapper
    StartupService.cs      "Start with Windows" via the HKCU Run key
    ShortcutService.cs     Start Menu shortcut so Windows Search finds the app
    NativeMethods.cs       Win32 interop (cursor position, DPI)
  Views/
    PopupWindow.xaml       frameless popup anchored above the taskbar
    SettingsWindow.xaml    settings form
.github/workflows/
  ci.yml                   restore + build on push to master and on PRs
  release.yml              publish single-file .exe to a GitHub Release
scripts/
  check-cli-contract.ps1   verifies the installed CLI still matches Interop.cs
```

## Build and run

Always pass `-p:Platform=x64` (or `arm64`). The project declares those two
platforms only, and every command in CI and in the README specifies one.

```powershell
dotnet restore AiUsageBar.sln
dotnet build AiUsageBar.sln -c Release -p:Platform=x64
dotnet run --project AiUsageBar/AiUsageBar.csproj -p:Platform=x64
```

Or open `AiUsageBar.sln` in Visual Studio, set the platform to x64, press F5.

To exercise the app you also need `ai-usagebar` on `PATH`
(`cargo install ai-usagebar`). Without it the app still starts and shows a
"System Error" card explaining the binary is missing.

`dotnet-install.ps1` at the root is the stock Microsoft SDK installer kept for
convenience. CI does not use it (it uses `actions/setup-dotnet`).

## Tests

There are none, deliberately. An empty `AiUsageBar.Tests` project was inherited
from the fork and never contained a single test, so it was removed along with
its solution entries and the CI `Test` step. If you reintroduce tests, note that
the app under test is a WPF assembly, so the test project needs `UseWPF`.

## Configuration

Read from `%APPDATA%\ai-usagebar\config\config.toml`. Missing file or parse
error falls back to defaults, never an exception.

The app owns only two settings:

- `poll_seconds`: refresh interval, default 60, floor of 15.
- `[ui] primary`: which vendor leads the tooltip and popup.

Everything else in that file (providers, API keys, credentials) belongs to the
Rust CLI. See `config.example.toml`.

## Tracking the upstream CLI

Validated against **`ai-usagebar` 1.0.3**. The upstream moves fast (ten releases
in the three weeks before that version, including a `0.22.0` to `1.0.0` jump),
so treat schema drift as expected rather than exceptional.

Update the CLI and re-check the contract:

```powershell
cargo install ai-usagebar --force
ai-usagebar --version
pwsh ./scripts/check-cli-contract.ps1
```

The check compares the live JSON against the fields in `Interop.cs`, the
severity strings in `SeverityRules.Parse`, and the section types `Renderer.cs`
handles. It exits non-zero when something the app depends on is gone.

Failure modes ranked by how they show up:

- **Loud and safe**: the command or its flags change. `Poller` catches it and
  renders a "System Error" card. Nothing to design around.
- **Silent**: a field is renamed. `System.Text.Json` ignores unknown fields and
  defaults missing ones, so the vendor renders as `ready` with no bars. This is
  what the contract script exists to catch.
- **Cosmetic**: a new vendor id appears. The tooltip tag falls back to the first
  three letters of the id. `Renderer.cs` has short tags for 7 of the 16 vendors
  the CLI supports; extending that list is optional polish.

`usage --json` carries no `schema_version` (only `settings show` does), so there
is no version field to branch on. The script is the substitute.

## Versioning and release

CalVer `YEAR.MONTH.REVISION`, month without a leading zero, revision restarting
at 1 whenever the year or month changes.

`<Version>` in `AiUsageBar/AiUsageBar.csproj` is the **single source of truth**.
`AssemblyVersion` and `FileVersion` derive from it, and `release.yml` reads it
to name the artifact and tag the release.

Two ways to release, both from `master`:

1. Push a matching tag: `git tag v2026.8.1 && git push origin v2026.8.1`.
2. Run the `release` workflow manually from the Actions tab.

`release.yml` fails the run when a pushed tag disagrees with `<Version>`, so
bump the `.csproj` first and tag second.

## Conventions

- Default branch is `master`.
- Conventional Commits in English, lowercase title, body as a hyphen list with
  past-tense verbs.
- **No em dashes or en dashes anywhere**, including code comments and docs. Use
  a comma, colon, parentheses, or a new sentence.
- Comments explain *why*, not *what*. The existing comments carry the reasoning
  behind non-obvious decisions; preserve that when editing around them.
- Nullable reference types and implicit usings are enabled.

## Gotchas

**The CLI reports vendors the user never configured.** `usage --json` returns
every candidate vendor, and unconfigured ones come back with status `error`
("no API key", missing credential file). Since `GetWorstSeverity` maps any
non-`ready` status to `Critical`, rendering them all would pin the tray icon to
red permanently. `Renderer.ShouldShow` exists exactly to prevent that: it keeps
entries that are `ready`, the primary vendor (so a real outage still surfaces),
and the synthetic system entry. **Do not delete this filter.** It was removed
once by accident and had to be restored.

**An unrecognized severity must never read as healthy.** `SeverityRules.Parse`
maps an unknown string to `Severity.Unknown` (grey), not `Severity.Low` (green),
and both `GetWorstSeverity` and `Render` propagate a single `Unknown` instead of
letting a healthy sibling metric mask it. The reason: `Unknown` sorts below
`Low` in the enum, so a naive `Max()` would hide it. If the CLI renames a
severity level, the tray must say "no idea", never "all good" over a maxed-out
quota.

**Publish flags live in the workflow, not the `.csproj`.** `SelfContained`,
`PublishSingleFile` and the RID are passed only on the `dotnet publish` command
line. Putting them in the `.csproj` would force a RID onto every `dotnet build`
and break it.

**`Config.Save()` must preserve unknown keys.** The TOML file is shared with the
Rust CLI, which stores credentials there. Saving through a plain model would
delete the user's API keys, so saving goes through a `TomlTable` that keeps
properties the C# model does not know about.

**The CLI has an API for native frontends.** `ai-usagebar settings show` prints
non-secret JSON including a `configured` flag per vendor, `primary`, and
`primary_choices`; `ai-usagebar settings apply` accepts a JSON patch on stdin.
The settings window currently writes the TOML directly instead. Migrating to
these commands would remove the C# side's knowledge of the file format.

**Windows only.** WPF requires Windows 10 2004 (19041) or later. There is no
cross-platform build.
