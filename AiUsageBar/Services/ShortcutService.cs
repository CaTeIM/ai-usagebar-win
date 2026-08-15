using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AiUsageBar.Services;

/// <summary>Creates a per-user Start Menu shortcut so the app is findable in
/// Windows Search. Unpackaged single-file <c>.exe</c>s don't show up there on
/// their own — Search indexes <c>.lnk</c> shortcuts, not loose executables — so
/// we drop one in <c>%APPDATA%\Microsoft\Windows\Start Menu\Programs</c> on
/// first run. Per-user, so it needs no administrator rights. Best-effort:
/// failures never crash startup.</summary>
public static class ShortcutService
{
    private const string ShortcutFileName = "AI Usage Bar.lnk";
    private const string Description = "AI plan usage in the system tray";

    private static string ShortcutPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), ShortcutFileName);

    /// <summary>Path of the running executable — the shortcut's target.</summary>
    private static string ExePath => Environment.ProcessPath ?? "";

    /// <summary>Create the Start Menu shortcut when missing (or when it points at
    /// a stale path, e.g. the exe moved). Idempotent and best-effort.</summary>
    public static void EnsureStartMenuShortcut()
    {
        try
        {
            var exe = ExePath;
            if (string.IsNullOrEmpty(exe)) return;

            var path = ShortcutPath;
            if (File.Exists(path) && TargetOf(path) is { } t
                && string.Equals(t, exe, StringComparison.OrdinalIgnoreCase))
                return; // already points here — nothing to do

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var link = (IShellLinkW)new ShellLink();
            link.SetPath(exe);
            link.SetWorkingDirectory(Path.GetDirectoryName(exe) ?? "");
            link.SetDescription(Description);
            link.SetIconLocation(exe, 0);
            ((IPersistFile)link).Save(path, true);
        }
        catch
        {
            // Best-effort — a tray app that can't write its shortcut still runs.
        }
    }

    /// <summary>Resolve the target path an existing shortcut points at, or null.</summary>
    private static string? TargetOf(string lnkPath)
    {
        try
        {
            var link = (IShellLinkW)new ShellLink();
            ((IPersistFile)link).Load(lnkPath, 0);
            var sb = new StringBuilder(260);
            link.GetPath(sb, sb.Capacity, out _, 0);
            return sb.Length == 0 ? null : sb.ToString();
        }
        catch
        {
            return null;
        }
    }

    // -- COM interop: IShellLinkW + IPersistFile (shell32 ShellLink) ----------

    [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink { }

    [ComImport,
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
     Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath,
                     out WIN32_FIND_DATAW pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath,
                             out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport,
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
     Guid("0000010B-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        [PreserveSig] int IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
                  [MarshalAs(UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WIN32_FIND_DATAW
    {
        public uint dwFileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string cFileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)] public string cAlternateFileName;
    }
}
