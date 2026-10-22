using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Rat;

public static class DarkTitleBar
{
    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void Apply(Window window)
    {
        if (window.IsLoaded)
        {
            Set(window);
            return;
        }
        window.SourceInitialized += (_, _) => Set(window);
    }

    static void Set(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;
            int on = 1;
            if (DwmSetWindowAttribute(hwnd, 20, ref on, sizeof(int)) != 0)
                DwmSetWindowAttribute(hwnd, 19, ref on, sizeof(int));
        }
        catch { }
    }
}
