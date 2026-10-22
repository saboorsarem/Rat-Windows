using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace Rat;

public sealed class RuleRow : INotifyPropertyChanged
{
    public Rule Rule { get; }
    public RuleRow(Rule r) => Rule = r;

    public bool Enabled
    {
        get => Rule.Enabled;
        set { Rule.Enabled = value; Changed(); }
    }
    public string Action => Rule.Action.ToString();
    public string AppDisplay => Rule.AppPath ?? Rule.AppName ?? "any app";
    public string? Server => Rule.Server;
    public string PortDisplay => Rule.Port?.ToString() ?? "any";
    public string Lifetime => Rule.Lifetime.ToString();
    public string? Description => Rule.Description;

    public event PropertyChangedEventHandler? PropertyChanged;
    void Changed() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled)));
}

public partial class RulesWindow : Window
{
    readonly Store _store;

    public RulesWindow(Store store)
    {
        InitializeComponent();
        _store = store;
        Reload();
        btnAdd.Click += (_, _) =>
        {
            var w = new RuleEditWindow { Owner = this };
            if (w.ShowDialog() == true && w.Result != null) { _store.Add(w.Result); Reload(); }
        };
        btnDelete.Click += (_, _) =>
        {
            if (grid.SelectedItem is not RuleRow row) return;
            _store.Remove(row.Rule);
            if (IPAddress.TryParse(row.Rule.Server, out var ip)) Blocker.Unblock(ip);
            Reload();
        };
        btnExport.Click += (_, _) =>
        {
            var dlg = new Microsoft.Win32.SaveFileDialog { FileName = "rules.json", Filter = "JSON|*.json" };
            if (dlg.ShowDialog() == true)
                File.WriteAllText(dlg.FileName, JsonSerializer.Serialize(_store.Forever, new JsonSerializerOptions { WriteIndented = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } }));
        };
        btnImport.Click += (_, _) =>
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "JSON|*.json" };
            if (dlg.ShowDialog() != true) return;
            try
            {
                var rules = JsonSerializer.Deserialize<List<Rule>>(File.ReadAllText(dlg.FileName), new JsonSerializerOptions { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } });
                if (rules != null) _store.ReplaceAll(rules);
            }
            catch (System.Exception ex) { MessageBox.Show(this, "Import failed: " + ex.Message, Title); }
            Reload();
        };
    }

    void Reload()
    {
        grid.ItemsSource = _store.All.Select(r => new RuleRow(r)).ToList();
    }
}
