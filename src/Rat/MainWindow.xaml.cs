using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Rat;

public sealed class ProcRow
{
    public string Name { get; init; } = "";
    public string? Path { get; init; }
    public int Pid { get; init; }
    public int Connections { get; init; }
    public string Servers { get; init; } = "";
}

public sealed class ConnRow
{
    public string Remote { get; init; } = "";
    public string? Host { get; set; }
    public ushort Port { get; init; }
    public string State { get; init; } = "";
    public DateTime Since { get; init; }
    public IPAddress Ip { get; init; } = IPAddress.None;
    public int Pid { get; init; }
    public string ProcessName { get; init; } = "";
    public string? ProcessPath { get; init; }
}

public partial class MainWindow : Window
{
    readonly Store _store = Store.Load();
    readonly ProcessCache _cache = new();
    readonly Throughput _tp = new();
    readonly Dictionary<string, DateTime> _since = new();
    readonly HashSet<string> _seen = new();
    readonly HashSet<string> _pending = new();
    readonly HashSet<string> _denied = new();
    readonly HashSet<IPAddress> _blocked = new();
    readonly Dictionary<IPAddress, string> _hosts = new();
    readonly HashSet<IPAddress> _resolving = new();
    readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(2) };
    List<ConnRow> _rows = new();
    bool _busy;
    bool _firstTick = true;

    public MainWindow()
    {
        InitializeComponent();
        chkAlert.IsChecked = _store.AlertMode;
        chkAlert.Checked += (_, _) => { _store.AlertMode = true; _store.SaveSettings(); };
        chkAlert.Unchecked += (_, _) => { _store.AlertMode = false; _store.SaveSettings(); };
        btnRules.Click += (_, _) => new RulesWindow(_store) { Owner = this }.ShowDialog();
        gridApps.SelectionChanged += (_, _) => RefreshConnGrid();
        _timer.Tick += (_, _) => Tick();
        Loaded += (_, _) => _timer.Start();
        Closing += OnClosing;
    }

    void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!App.Exiting) { e.Cancel = true; Hide(); }
    }

    public void ShowUp()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    void Tick()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            _tp.Update();
            tbThroughput.Text = _tp.Text;

            var snapshot = Connections.Snapshot(_cache);
            var now = DateTime.Now;
            var rows = new List<ConnRow>(snapshot.Count);
            foreach (var c in snapshot)
            {
                string key = Key(c);
                if (!_since.TryGetValue(key, out var since)) { since = now; _since[key] = since; }
                if (_seen.Add(key)) Evaluate(c);
                rows.Add(new ConnRow
                {
                    Remote = c.Remote.ToString(),
                    Ip = c.Remote,
                    Port = c.RemotePort,
                    State = c.State,
                    Since = since,
                    Pid = c.Pid,
                    ProcessName = c.ProcessName,
                    ProcessPath = c.ProcessPath,
                    Host = _hosts.TryGetValue(c.Remote, out var h) ? h : null,
                });
                ResolveHost(c.Remote);
            }
            _rows = rows;

            var procs = snapshot
                .GroupBy(c => new { c.Pid, c.ProcessName, c.ProcessPath })
                .Select(g =>
                {
                    var servers = g.Select(x => x.Remote.ToString()).Distinct().ToList();
                    return new ProcRow
                    {
                        Pid = g.Key.Pid,
                        Name = g.Key.ProcessName,
                        Path = g.Key.ProcessPath,
                        Connections = g.Count(),
                        Servers = string.Join(", ", servers.Take(4)) + (servers.Count > 4 ? ", ..." : ""),
                    };
                })
                .OrderByDescending(p => p.Connections)
                .ToList();

            var selPid = (gridApps.SelectedItem as ProcRow)?.Pid;
            gridApps.ItemsSource = procs;
            if (selPid is int s) gridApps.SelectedItem = procs.FirstOrDefault(p => p.Pid == s);
            RefreshConnGrid();
            tbStatus.Text = $"{snapshot.Count} connections, {procs.Count} apps"
                + (Blocker.IsAdmin() ? " - admin" : " - run as administrator to enforce");
            _firstTick = false;
        }
        catch (Exception ex)
        {
            tbStatus.Text = "error: " + ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    void RefreshConnGrid()
    {
        var sel = gridApps.SelectedItem as ProcRow;
        gridConns.ItemsSource = sel == null ? _rows : _rows.Where(r => r.Pid == sel.Pid).ToList();
    }

    static string Key(Connection c) => $"{c.Pid}|{c.Remote}|{c.RemotePort}|{c.LocalPort}";

    void Evaluate(Connection c)
    {
        _hosts.TryGetValue(c.Remote, out var host);
        var rule = RulesEngine.Match(_store.All, c.ProcessPath, c.ProcessName, c.Remote.ToString(), c.RemotePort, host);
        if (rule != null)
        {
            if (rule.Action == RuleAction.Deny) EnforceDeny(c);
            return;
        }
        if (!_store.AlertMode || _firstTick) return; // silent allow is the default, like the macOS app
        if (!_pending.Add(Key(c))) return;
        var win = new AlertWindow(c) { Owner = this };
        win.ShowDialog();
        _pending.Remove(Key(c));
        var saved = win.ResultRule;
        if (saved != null)
        {
            _store.Add(saved);
            if (saved.Action == RuleAction.Deny) EnforceDeny(c);
        }
        else if (win.ResultAction == RuleAction.Deny)
        {
            EnforceDeny(c);
        }
        tbStatus.Text = $"decided {c.ProcessName} -> {c.Remote}";
    }

    void EnforceDeny(Connection c)
    {
        if (_blocked.Add(c.Remote)) Blocker.Block(c.Remote);
        // cut the live connection once
        if (_denied.Add(Key(c))) Connections.Close(c);
    }

    async void ResolveHost(IPAddress ip)
    {
        if (_hosts.ContainsKey(ip) || !_resolving.Add(ip)) return;
        string? host = null;
        try
        {
            var lookup = Dns.GetHostEntryAsync(ip);
            var done = await Task.WhenAny(lookup, Task.Delay(1500));
            if (done == lookup && lookup.IsCompletedSuccessfully) host = lookup.Result?.HostName;
        }
        catch { }
        Dispatcher.Invoke(() =>
        {
            _resolving.Remove(ip);
            if (!string.IsNullOrEmpty(host)) _hosts[ip] = host;
            RefreshConnGrid();
        });
    }
}
