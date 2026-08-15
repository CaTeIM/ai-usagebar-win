# Changelog

## 0.3.0

UI-stack rewrite plus new convenience features. Ships as a single
self-contained `.exe` built by GitHub Actions — no Windows App SDK runtime
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
