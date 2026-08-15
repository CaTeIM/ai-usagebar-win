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
        Severity.Low => (0x4c, 0xaf, 0x50),       // green
        Severity.Mid => (0xff, 0xc1, 0x07),       // amber
        Severity.High => (0xff, 0x98, 0x00),      // orange
        Severity.Critical => (0xf4, 0x43, 0x36),  // red
        _ => (0x4c, 0xaf, 0x50),
    };

    /// <summary>A 32x32 rounded-square icon tinted by severity. Icons are cached
    /// for the process lifetime (only four ever exist).</summary>
    public static Icon For(Severity severity)
    {
        if (Cache.TryGetValue(severity, out var cached)) return cached;

        var (r, g, b) = Rgb(severity);
        using var bmp = new Bitmap(Size, Size);
        using (var gfx = Graphics.FromImage(bmp))
        {
            gfx.SmoothingMode = SmoothingMode.AntiAlias;
            gfx.Clear(Color.Transparent);

            using var path = RoundedRect(0, 0, Size, Size, 6);
            // 2px darker border for definition against any taskbar color.
            using var borderBrush = new SolidBrush(Color.FromArgb(255, r / 2, g / 2, b / 2));
            gfx.FillPath(borderBrush, path);

            using var innerPath = RoundedRect(2, 2, Size - 4, Size - 4, 4);
            using var fillBrush = new SolidBrush(Color.FromArgb(255, r, g, b));
            gfx.FillPath(fillBrush, innerPath);
        }

        // GetHicon's handle is intentionally leaked: the icon lives for the
        // whole process and there are only four of them.
        var icon = Icon.FromHandle(bmp.GetHicon());
        Cache[severity] = icon;
        return icon;
    }

    private static GraphicsPath RoundedRect(int x, int y, int w, int h, int radius)
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
