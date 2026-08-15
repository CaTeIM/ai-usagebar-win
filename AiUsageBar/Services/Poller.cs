using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using AiUsageBar.Models;

namespace AiUsageBar.Services;

/// <summary>Background polling loop that executes `ai-usagebar usage --json`.</summary>
public sealed class Poller : IDisposable
{
    private readonly Dispatcher _ui;
    private readonly SemaphoreSlim _wake = new(0, 1);
    private readonly CancellationTokenSource _cts = new();

    /// <summary>Raised on the UI thread after each poll completes.</summary>
    public event Action<Config, UsageJsonRoot>? Updated;

    public Poller(Dispatcher uiThread) => _ui = uiThread;

    public void Start() => _ = LoopAsync(_cts.Token);

    public void TriggerRefresh()
    {
        try { _wake.Release(); }
        catch (SemaphoreFullException) { /* a refresh is already pending */ }
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var cfg = Config.Load();
            var jsonRoot = await FetchJsonAsync(ct).ConfigureAwait(false);

            if (jsonRoot != null)
            {
                _ui.BeginInvoke(() => Updated?.Invoke(cfg, jsonRoot));
            }

            try
            {
                await _wake.WaitAsync(cfg.PollInterval(), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static async Task<UsageJsonRoot?> FetchJsonAsync(CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ai-usagebar",
                Arguments = "usage --json",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Environment = { ["NO_COLOR"] = "1" } // Ensure clean JSON
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
            await process.WaitForExitAsync(ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(output)) return null;

            return JsonSerializer.Deserialize<UsageJsonRoot>(output);
        }
        catch
        {
            return null; // Return null on execution/parsing error
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _wake.Dispose();
    }
}
