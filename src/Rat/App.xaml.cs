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
        var menu = new WF.ContextMenuStrip
        {
            BackColor = System.Drawing.Color.Black,
            ForeColor = System.Drawing.Color.White,
            ShowImageMargin = false,
            Renderer = new WF.ToolStripProfessionalRenderer(new DarkMenuColors()) { RoundedEdges = false },
        };
        menu.Items.Add("Open", null, (_, _) => ShowMain());
        menu.Items.Add("Exit", null, (_, _) => ExitApp());
        foreach (WF.ToolStripItem item in menu.Items) item.ForeColor = System.Drawing.Color.White;
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

sealed class DarkMenuColors : WF.ProfessionalColorTable
{
    static readonly System.Drawing.Color Bg = System.Drawing.Color.Black;
    static readonly System.Drawing.Color Surface = System.Drawing.Color.FromArgb(17, 17, 17);
    static readonly System.Drawing.Color Border = System.Drawing.Color.FromArgb(51, 51, 51);
    static readonly System.Drawing.Color Hover = System.Drawing.Color.FromArgb(42, 42, 42);

    public override System.Drawing.Color ToolStripDropDownBackground => Bg;
    public override System.Drawing.Color ImageMarginGradientBegin => Surface;
    public override System.Drawing.Color ImageMarginGradientMiddle => Surface;
    public override System.Drawing.Color ImageMarginGradientEnd => Surface;
    public override System.Drawing.Color MenuBorder => Border;
    public override System.Drawing.Color MenuItemBorder => Border;
    public override System.Drawing.Color MenuItemSelected => Hover;
    public override System.Drawing.Color MenuItemSelectedGradientBegin => Hover;
    public override System.Drawing.Color MenuItemSelectedGradientEnd => Hover;
    public override System.Drawing.Color MenuItemPressedGradientBegin => Hover;
    public override System.Drawing.Color MenuItemPressedGradientEnd => Hover;
    public override System.Drawing.Color SeparatorDark => Border;
    public override System.Drawing.Color SeparatorLight => Border;
}
