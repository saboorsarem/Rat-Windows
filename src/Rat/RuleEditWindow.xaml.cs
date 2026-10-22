using System.Windows;
using System.Windows.Controls;

namespace Rat;

public partial class RuleEditWindow : Window
{
    public Rule? Result { get; private set; }

    public RuleEditWindow()
    {
        InitializeComponent();
        cmbAction.ItemsSource = Enum.GetValues<RuleAction>();
        cmbAction.SelectedIndex = 1; // deny
        cmbLife.ItemsSource = new[] { RuleLifetime.Session, RuleLifetime.Forever };
        cmbLife.SelectedIndex = 1;
        btnOk.Click += Ok;
    }

    void Ok(object sender, RoutedEventArgs e)
    {
        if (cmbAction.SelectedItem is not RuleAction action) return;
        Result = new Rule
        {
            Action = action,
            Lifetime = (RuleLifetime)cmbLife.SelectedItem,
            AppPath = string.IsNullOrWhiteSpace(tbApp.Text) ? null : tbApp.Text.Trim(),
            Server = string.IsNullOrWhiteSpace(tbServer.Text) ? null : tbServer.Text.Trim(),
            Port = ushort.TryParse(tbPort.Text, out var p) ? p : null,
            Description = "manual",
        };
        DialogResult = true;
        Close();
    }
}
