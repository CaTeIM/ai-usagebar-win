# Changelog

Versions follow CalVer (`YEAR.MONTH.REVISION`). Earlier entries use the
SemVer scheme this project shipped with before the rewrite.

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

### Fixed
- Restored the build: the renderer still called `Config.IsConfiguredId`, removed
  during the legacy cleanup.

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
