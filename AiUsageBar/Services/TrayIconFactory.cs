using System.Drawing;
using System.Drawing.Drawing2D;
using AiUsageBar.Models;

namespace AiUsageBar.Services;

/// <summary>Generates the tray icon bitmap in code (no asset files) so the build
/// is self-contained. The fill color encodes the worst-case severity, giving an
/// at-a-glance signal in the notification area. Mirrors the Rust <c>tray.rs</c>.</summary>
public static class TrayIconFactory
{
    private const int Size = 32;

    private static readonly Dictionary<Severity, Icon> Cache = new();

    private static (int R, int G, int B) Rgb(Severity s) => s switch
    {
        Severity.Unknown => (0x9e, 0x9e, 0x9e),   // grey
        Severity.Low => (0x4c, 0xaf, 0x50),       // green
        Severity.Mid => (0xff, 0xc1, 0x07),       // amber
        Severity.High => (0xff, 0x98, 0x00),      // orange
        Severity.Critical => (0xf4, 0x43, 0x36),  // red
        _ => (0x9e, 0x9e, 0x9e),
    };

    /// <summary>Relative heights of the three bars, as a fraction of the tallest.
    /// Mirrors scripts/generate-icon.py so the tray and the .exe icon match.</summary>
    private static readonly float[] BarHeights = { 0.47f, 0.73f, 1.00f };

    /// <summary>A 32x32 three-bar icon tinted by severity. A plain square read as
    /// a missing-icon placeholder; the rising bars say "usage meter" even at 16px.
    /// Icons are cached for the process lifetime (only five ever exist).</summary>
    public static Icon For(Severity severity)
    {
        if (Cache.TryGetValue(severity, out var cached)) return cached;

        var (r, g, b) = Rgb(severity);
        using var bmp = new Bitmap(Size, Size);
        using (var gfx = Graphics.FromImage(bmp))
        {
            gfx.SmoothingMode = SmoothingMode.AntiAlias;
            gfx.Clear(Color.Transparent);

            using var fill = new SolidBrush(Color.FromArgb(255, r, g, b));

            const float barWidth = Size * 0.16f;
            const float gap = Size * 0.08f;
            const float baseline = Size * 0.80f;
            const float tallest = Size * 0.60f;
            const float radius = barWidth * 0.28f;

            var x = (Size - (barWidth * 3 + gap * 2)) / 2f;
            foreach (var height in BarHeights)
            {
                var top = baseline - tallest * height;
                using var path = RoundedRect(x, top, barWidth, baseline - top, radius);
                gfx.FillPath(fill, path);
                x += barWidth + gap;
            }
        }

        // GetHicon's handle is intentionally leaked: the icon lives for the
        // whole process and there is one per severity.
        var icon = Icon.FromHandle(bmp.GetHicon());
        Cache[severity] = icon;
        return icon;
    }

    private static GraphicsPath RoundedRect(float x, float y, float w, float h, float radius)
    {
        var d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + w - d, y, d, d, 270, 90);
        path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
        path.AddArc(x, y + h - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
