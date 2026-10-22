using System.Threading;
using System.Windows;
using Application = System.Windows.Application;
using WF = System.Windows.Forms;

namespace Rat;

public partial class App : Application
{
    public static bool Exiting { get; private set; }
    WF.NotifyIcon? _tray;
    Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "Rat-Windows.SingleInstance", out var first);
        if (!first) { Shutdown(); return; }
        base.OnStartup(e);

        _tray = new WF.NotifyIcon
        {
            Icon = TrayIcon(),
            Text = "Rat - outbound connection monitor",
            Visible = true,
        };
        var menu = new WF.ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => ShowMain());
        menu.Items.Add("Exit", null, (_, _) => ExitApp());
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => ShowMain();
    }

    static System.Drawing.Icon TrayIcon()
    {
        try
        {
            var path = Environment.ProcessPath;
            var icon = path == null ? null : System.Drawing.Icon.ExtractAssociatedIcon(path);
            if (icon != null) return icon;
        }
        catch { }
        return System.Drawing.SystemIcons.Shield;
    }

    void ShowMain()
    {
        if (MainWindow is MainWindow m) m.ShowUp();
    }

    void ExitApp()
    {
        Exiting = true;
        _tray?.Dispose();
        _tray = null;
        MainWindow?.Close();
        Shutdown();
    }
}
