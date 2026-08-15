# ai-usagebar-win

> **Nota de Autoria/Inspiração:** Este projeto é uma evolução estrutural baseada no excelente projeto original [ai-usagebar-win](https://github.com/FranzoiDev/ai-usagebar-win) criado por Gabriel Franzoi. A interface gráfica (WPF) e o conceito foram mantidos, mas a arquitetura interna foi refatorada para atuar como um wrapper de um core executável em Rust, delegando a lógica de API e credenciais ao binário nativo.

Windows system-tray app that shows AI plan usage for Anthropic (Claude),
OpenAI (Codex), Z.AI (GLM), OpenRouter, and DeepSeek.

Built with **C# and WPF** on .NET 8, styled with
[`WPF-UI`](https://github.com/lepoco/wpfui) for a Fluent look (Mica backdrop,
dark theme, modern controls). The tray icon uses
[`H.NotifyIcon`](https://github.com/HavenDV/H.NotifyIcon); config is TOML via
[`Tomlyn`](https://github.com/xoofx/Tomlyn) and stays compatible with the Linux
[`ai-usagebar`](https://github.com/akitaonrails/ai-usagebar) Waybar widget. The
popup and settings windows are native XAML.

> **History:** this started as a Rust + raw-Win32 app, was rewritten in
> C#/WinUI 3, then moved to C#/WPF. WPF drops the dependency on the Windows App
> SDK runtime, so GitHub Actions can build a single self-contained `.exe`. The
> data layer (vendor endpoints, credential parsing, severity model) is a
> faithful port across all three — see the git history for the earlier versions.

By default the app is read-only: it reads the access tokens the `claude` /
`codex` CLIs already wrote to disk and never refreshes or rewrites them, so it
cannot log you out. An expired token shows a "re-login" hint instead.

**Optional token refresh.** You can opt in (Settings → *Auto-refresh Claude /
Codex tokens*, or `refresh_tokens = true` in the config) to let the app refresh
a near-expiry OAuth token and write it back to the CLI credential file. This
rotates the token the CLI shares, so a `claude` / `codex` session signed in
elsewhere may need to re-login — the setting warns about this before you enable
it. It stays off unless you turn it on.

## Screenshots

| Popup — click the tray icon | Tray tooltip — hover |
| :---: | :---: |
| <img src="screenshots/click.PNG" alt="Popup with per-provider usage cards" width="300"> | <img src="screenshots/hover.PNG" alt="Tray tooltip, one line per provider" width="220"> |

| Settings |
| :---: |
| <img src="screenshots/settings.PNG" alt="Settings window" width="440"> |

## UI

- **Hover** the tray icon for a one-line-per-provider tooltip.
- **Click** the tray icon for a popup with a card and progress bars per
  provider. Only providers with an identified key/credential are shown.
- **Settings** (button in the popup) opens a window to enable providers, manage
  API keys, set the refresh interval, choose the primary provider, opt into
  **Auto-refresh Claude / Codex tokens**, and toggle **Start with Windows**.
  Changes are written to `config.toml` (except **Start with Windows**, which
  goes to the registry) and applied without a restart.
- **Quit** (button in the popup) exits the whole process.

The icon color tracks worst-case usage: green <50%, yellow >=50%, orange >=75%,
red >=90%.

## Providers

| Provider | Source | Setup |
|---|---|---|
| Anthropic | `%USERPROFILE%\.claude\.credentials.json` | run `claude` once |
| OpenAI | `%USERPROFILE%\.codex\auth.json` | run `codex login` once |
| Z.AI | `ZAI_API_KEY` or `[zai] api_key` | set the key |
| OpenRouter | `OPENROUTER_API_KEY` or `[openrouter] api_key` | set the key |
| DeepSeek | `DEEPSEEK_API_KEY` or `[deepseek] api_key` | set the key, enable in config |

Set API keys via environment variable:

```powershell
setx ZAI_API_KEY "your-key"
setx OPENROUTER_API_KEY "your-key"
```

`setx` only affects new terminals. A provider that isn't configured shows
"login needed" and the others keep working.

## Config

Optional. Copy `config.example.toml` to `%APPDATA%\ai-usagebar\config.toml`.

- `poll_seconds`: refresh interval, default 60, minimum 15.
- `refresh_tokens`: opt-in OAuth token refresh, default false (see above).
- `[ui] primary`: provider shown first in the tooltip/popup.
- per-provider `enabled` and inline `api_key`.

Same format as the Linux `ai-usagebar`, so an existing config works. The
Settings window edits this same file, so hand-edits and the UI stay in sync.
"Start with Windows" is the one setting kept outside the TOML — it lives in the
per-user `HKCU\...\Run` registry key.

## Build

Requires:

- **.NET 8 SDK**
- **Windows 10 2004 (19041) or later** — WPF is Windows-only.
- Optional: **Visual Studio 2022** with the *.NET Desktop Development* workload.
  The CLI builds need only the .NET 8 SDK; no Windows App SDK / WinUI components.

```powershell
# from the repo root
dotnet restore AiUsageBar.sln
dotnet build  AiUsageBar.sln -c Release -p:Platform=x64
dotnet test   AiUsageBar.Tests/AiUsageBar.Tests.csproj -c Release -p:Platform=x64

# run
dotnet run --project AiUsageBar/AiUsageBar.csproj -p:Platform=x64
```

Or open `AiUsageBar.sln` in Visual Studio, set the platform to **x64**, and
press F5.

## Deploy

Unlike the old WinUI build, WPF needs no separate Windows App SDK runtime, so it
publishes to a **single self-contained `.exe`** that runs on a clean machine.
The self-contained / single-file / RID flags are passed at *publish* time only
(setting them in the `.csproj` would force a RID on every `dotnet build`/`test`
and break them):

```powershell
dotnet publish AiUsageBar/AiUsageBar.csproj -c Release -p:Platform=x64 `
  -r win-x64 --self-contained true -p:PublishSingleFile=true
# -> AiUsageBar/bin/x64/Release/net8.0-windows10.0.19041.0/win-x64/publish/ai-usagebar-win.exe
```

Pushing a version tag (e.g. `git tag v0.3.0 && git push origin v0.3.0`) runs the
`release` GitHub Actions workflow, which publishes the same build and attaches
the `.exe` to a GitHub Release.

On first run the app adds a **Start Menu shortcut** (per-user, no admin needed),
so you can find and launch it from Windows Search by typing "AI Usage Bar".
Only one instance runs at a time — launching it again while it's in the tray
just reopens the popup instead of adding a second icon. **Quit** (in the popup)
closes it.

To start it with Windows, use the **Start with Windows** toggle in Settings (or
put a shortcut to `ai-usagebar-win.exe` in `shell:startup`).

## Layout

| Path | Purpose |
|---|---|
| `Models/Usage.cs` | snapshot model, severity tiers, countdown formatting |
| `Models/VendorId.cs` | provider ids + display/slug helpers |
| `Models/VendorReport.cs` | per-vendor poll result (Ok / NeedsLogin / Error) |
| `Models/ViewModels.cs` | popup + settings view-models bound by XAML |
| `Services/Config.cs` | TOML config load/save, API-key and path resolution |
| `Services/Creds.cs` | Claude/Codex credential readers; opt-in token refresh + write-back |
| `Services/OAuthClient.cs` | OAuth token-refresh calls (Anthropic + OpenAI) |
| `Services/Vendors/` | one file per provider: endpoint, parse, snapshot |
| `Services/Renderer.cs` | reports -> tooltip + popup/settings view-models |
| `Services/TrayIconFactory.cs` | severity-tinted tray icon drawn in code |
| `Services/TrayService.cs` | H.NotifyIcon wrapper |
| `Services/StartupService.cs` | "Start with Windows" via the HKCU Run key |
| `Services/ShortcutService.cs` | Start Menu shortcut so Search can find the app |
| `Services/Poller.cs` | background polling loop with on-demand refresh |
| `Views/PopupWindow.xaml` | frameless popup anchored near the tray |
| `Views/SettingsWindow.xaml` | provider enable + API-key form (Fluent window) |
| `App.xaml.cs` | tray-first app wiring; single-instance + shortcut on first run |

## Endpoints

Undocumented, reverse-engineered, may change.

| Provider | Endpoint |
|---|---|
| Anthropic | `GET api.anthropic.com/api/oauth/usage` (header `anthropic-beta: oauth-2025-04-20`) |
| OpenAI | `GET chatgpt.com/backend-api/wham/usage` |
| Z.AI | `GET api.z.ai/api/monitor/usage/quota/limit` (key in `Authorization`, no `Bearer`) |
| OpenRouter | `GET openrouter.ai/api/v1/credits` and `/key` |
| DeepSeek | `GET api.deepseek.com/user/balance` |

## License

MIT. Data layer reverse-engineered from
[akitaonrails/ai-usagebar](https://github.com/akitaonrails/ai-usagebar).
