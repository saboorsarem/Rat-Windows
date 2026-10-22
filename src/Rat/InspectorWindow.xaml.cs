using System.Windows;

namespace Rat;

public partial class InspectorWindow : Window
{
    public InspectorWindow(string appName, List<ConnRow> rows)
    {
        InitializeComponent();
        DarkTitleBar.Apply(this);
        tbTitle.Text = appName;
        tbSub.Text = rows.Count == 0 ? "no active connections" : $"{rows.Count} active connections";
        grid.ItemsSource = rows;
    }
}
