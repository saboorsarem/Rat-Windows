using System.Windows;

namespace Rat;

public partial class AlertWindow : Window
{
    readonly Connection _c;
    public RuleAction ResultAction { get; private set; }
    public Rule? ResultRule { get; private set; }

    public AlertWindow(Connection c)
    {
        InitializeComponent();
        DarkTitleBar.Apply(this);
        _c = c;
        tbApp.Text = $"{c.ProcessName} (pid {c.Pid})";
        tbConn.Text = $"{c.ProcessPath ?? "path unknown"}  ->  {c.Remote}:{c.RemotePort}  [{c.State}]";
        btnAllow.Click += (_, _) => Finish(RuleAction.Allow);
        btnDeny.Click += (_, _) => Finish(RuleAction.Deny);
        Loaded += (_, _) => btnDeny.Focus();
    }

    void Finish(RuleAction action)
    {
        ResultAction = action;
        var life = rbOnce.IsChecked == true ? RuleLifetime.Once
                 : rbSession.IsChecked == true ? RuleLifetime.Session
                 : RuleLifetime.Forever;
        if (life != RuleLifetime.Once)
        {
            bool anyApp = cmbScope.SelectedIndex == 2;
            bool anyServer = cmbScope.SelectedIndex == 0;
            bool byPort = cmbScope.SelectedIndex == 3;
            ResultRule = new Rule
            {
                Action = action,
                Lifetime = life,
                AppPath = anyApp || _c.ProcessPath == null ? null : _c.ProcessPath,
                AppName = !anyApp && _c.ProcessPath == null ? _c.ProcessName : null,
                Server = anyServer ? null : _c.Remote.ToString(),
                Port = byPort ? _c.RemotePort : null,
                Description = $"{action.ToString().ToLower()} via alert",
            };
        }
        DialogResult = true;
        Close();
    }
}
