using System.Runtime.InteropServices;

namespace AiUsageBar.Services;

/// <summary>The small bit of Win32 still needed under WinUI: the cursor position
/// (to anchor the popup near the tray click) and the window DPI (to convert
/// effective pixels to physical pixels for AppWindow placement).</summary>
internal static class NativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(nint hwnd);
}
