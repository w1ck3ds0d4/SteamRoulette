using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SteamRoulette.App;

/// <summary>
/// Tints a window's native Windows 11 title bar to match the dark app theme using DWM,
/// so the caption blends into the app instead of showing the default light bar. Keeps the
/// native title bar (so maximize, snap, drag and resize all behave natively). No-ops on
/// older Windows that don't support these attributes.
/// </summary>
internal static class DarkTitleBar
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;

    public static void Apply(Window window)
    {
        if (new WindowInteropHelper(window).Handle != IntPtr.Zero)
            Set(window);
        else
            window.SourceInitialized += (_, _) => Set(window);
    }

    private static void Set(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            int on = 1;
            DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref on, sizeof(int));

            int caption = Bgr(0x0F, 0x12, 0x18); // matches the window background (#0F1218)
            DwmSetWindowAttribute(hwnd, DwmwaCaptionColor, ref caption, sizeof(int));

            int border = Bgr(0x22, 0x2C, 0x3A); // subtle panel-border colour
            DwmSetWindowAttribute(hwnd, DwmwaBorderColor, ref border, sizeof(int));

            int text = Bgr(0xC7, 0xD5, 0xE0); // light caption text
            DwmSetWindowAttribute(hwnd, DwmwaTextColor, ref text, sizeof(int));
        }
        catch
        {
            // Older Windows builds: leave the native title bar as-is.
        }
    }

    /// <summary>Pack R,G,B into a Win32 COLORREF (0x00BBGGRR).</summary>
    private static int Bgr(byte r, byte g, byte b) => r | (g << 8) | (b << 16);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);
}
