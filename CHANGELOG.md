# Changelog

Versions follow CalVer (`YEAR.MONTH.REVISION`). Earlier entries use the
SemVer scheme this project shipped with before the rewrite.

## 2026.8.4

### Added
- The app now ships with the `ai-usagebar` CLI inside it. Installing Rust and
  running `cargo install` is no longer required: download the `.exe` and it
  works. The bundled copy is extracted on first use and takes precedence over
  any copy already on `PATH`, so everyone runs the version the release was
  tested against. Each release records which CLI version it shipped, and the
  redistribution terms are in `THIRD-PARTY-NOTICES.md`.

## 2026.8.3

### Fixed
- Saving in the settings window no longer breaks the app. The refresh interval
  was written into the CLI's `config.toml` as `poll_seconds`, a key the CLI does
  not accept, and it then refused to parse the file at all, so every reading
  turned into "System Error". The interval now lives in
  `%APPDATA%\ai-usagebar-win\settings.toml`, and saving also removes the stray
  key from the CLI's file, repairing configs broken by earlier versions.

### Changed
- Settings are now split by owner: the refresh interval belongs to this app, and
  only `[ui] primary` is written into the CLI's config.

## 2026.8.2

### Fixed
- The popup no longer refuses to reopen. Closing it left an internal flag set, so
  every later tray click hid an already hidden window and the only way out was
  Task Manager.
- Output from the CLI is now decoded as UTF-8. Separators and accented text came
  through mangled (`Â·` instead of `·`) because the pipe was being read with the
  console code page.
- Usage bars no longer overlap their own text. The CLI's detail text grew into a
  full sentence and collided with the metric label; it now sits on its own line
  under each bar.

### Added
- The executable finally has an icon, three rising bars, instead of the generic
  Windows placeholder in Explorer, the taskbar and the installer.
- The tray icon uses that same three-bar shape rather than a plain square, while
  still being tinted by severity.

## 2026.8.1

### Changed
- Architecture rewrite: The app is now a C# WPF wrapper around the native Rust `ai-usagebar` binary.
- Delegated all network requests, API key management, and OAuth to the Rust CLI.
- Config handling now preserves unknown keys on save, so CLI-owned settings survive a round trip.
- Replaced em-dashes with hyphens across user-facing strings.
- Migrated versioning from SemVer to CalVer.

### Added
- 10-second timeout on the CLI process, preventing an indefinite hang.
- Captured `stderr` and surfaced it as a synthetic error entry, so a missing or
  failing binary is reported in the UI instead of failing silently.
- `Severity.Unknown` (grey icon) for the uninitialized state, distinguishing
  "not loaded yet" from "healthy".

- A script that checks the installed CLI against the JSON contract the app
  expects, so upstream schema changes are caught instead of failing silently.

### Fixed
- Restored the build: the renderer still called `Config.IsConfiguredId`, removed
  during the legacy cleanup.
- Vendors you never configured are no longer listed. The CLI reports every
  candidate vendor, and the unconfigured ones came back as errors, which kept the
  tray icon permanently red.
- An unrecognized severity from the CLI now shows grey instead of green, so a
  future upstream rename cannot make a maxed-out quota look healthy.

### Removed
- Obsolete OAuth and API key logic from config and view models.
- The empty `AiUsageBar.Tests` project, which never contained a test.

## 0.3.0

UI-stack rewrite plus new convenience features. Ships as a single
self-contained `.exe` built by GitHub Actions, no Windows App SDK runtime
needed.

### Changed
- Rewrote the app in **C# + WPF** (from the original Rust + Win32), styled with
  [WPF-UI](https://github.com/lepoco/wpfui) for a Fluent look (Mica backdrop,
  dark theme, modern controls).

### Added
- **Optional OAuth token refresh** for Claude/Codex (off by default): refreshes
  a near-expiry token and writes the rotated tokens back to the CLI credential
  files. The setting warns that it may sign out a CLI session.
- **Start with Windows** toggle (per-user `Run` registry key).
- **Start Menu shortcut** created on first run, so the app is findable in
  Windows Search.
- **Single-instance launch**: re-launching surfaces the existing popup instead
  of adding a second tray icon.

### Fixed
- The popup now anchors just above the taskbar instead of at the cursor height.

Earlier releases: <https://github.com/FranzoiDev/ai-usagebar-win/releases>
