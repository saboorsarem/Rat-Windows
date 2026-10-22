using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Path = System.Windows.Shapes.Path;
using Point = System.Windows.Point;

namespace Rat;

public sealed class ProcRow
{
    public string Name { get; init; } = "";
    public string? Path { get; init; }
    public int Pid { get; init; }
    public int Connections { get; init; }
    public string Servers { get; init; } = "";
    public ImageSource? Icon { get; init; }
    public bool DenyActive { get; init; }
    public bool AllowActive { get; init; }
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

public sealed class SummaryItem
{
    public string Name { get; init; } = "";
    public string Detail { get; init; } = "";
    public ImageSource? Icon { get; init; }
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
    readonly List<(double Down, double Up)> _samples = new();
    readonly ObservableCollection<SummaryItem> _topProcs = new();
    readonly ObservableCollection<SummaryItem> _topDomains = new();
    readonly ObservableCollection<SummaryItem> _topCountries = new();
    readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(2) };
    List<ConnRow> _rows = new();
    List<ProcRow> _allProcs = new();
    List<Connection> _snapshot = new();
    bool _busy;
    bool _firstTick = true;

    public MainWindow()
    {
        InitializeComponent();
        DarkTitleBar.Apply(this);
        var work = SystemParameters.WorkArea;
        Width = Math.Min(Width, Math.Max(MinWidth, work.Width - 40));
        Height = Math.Min(Height, Math.Max(MinHeight, work.Height - 40));
        chkAlert.IsChecked = _store.AlertMode;
        chkAlert.Checked += (_, _) => { _store.AlertMode = true; _store.SaveSettings(); };
        chkAlert.Unchecked += (_, _) => { _store.AlertMode = false; _store.SaveSettings(); };
        btnRules.Click += (_, _) => new RulesWindow(_store) { Owner = this }.ShowDialog();
        icProcs.ItemsSource = _topProcs;
        icDomains.ItemsSource = _topDomains;
        icCountries.ItemsSource = _topCountries;
        txtSearch.TextChanged += (_, _) =>
        {
            tbSearchHint.Visibility = txtSearch.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            RenderProcs();
        };
        gridApps.MouseDoubleClick += (_, _) => OpenInspector();
        _timer.Tick += (_, _) => Tick();
        Loaded += (_, _) =>
        {
            mapPath.Data = WorldMap.Build();
            if (!GeoIp.Available || mapPath.Data == null)
            {
                txtMapNote.Text = !GeoIp.Available
                    ? "Geo database is not bundled in this build. Run scripts/fetch-geoip.ps1 and rebuild."
                    : "World outline is missing from this build.";
                txtMapNote.Visibility = Visibility.Visible;
            }
            _timer.Start();
        };
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
            _snapshot = snapshot;
            var now = DateTime.Now;
            var rows = new List<ConnRow>(snapshot.Count);
            foreach (var c in snapshot)
            {
                var key = Key(c);
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

            var appRules = _store.All.Where(r => r.Server == null && r.Port == null).ToList();
            _allProcs = snapshot
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
                        Icon = IconCache.Get(g.Key.ProcessPath),
                        DenyActive = appRules.Any(r => r.Action == RuleAction.Deny && AppMatches(r, g.Key.ProcessPath, g.Key.ProcessName)),
                        AllowActive = appRules.Any(r => r.Action == RuleAction.Allow && AppMatches(r, g.Key.ProcessPath, g.Key.ProcessName)),
                    };
                })
                .OrderByDescending(p => p.Connections)
                .ThenBy(p => p.Name)
                .ToList();

            RenderProcs();
            DrawArcs(snapshot);
            UpdateSummary(snapshot, _allProcs);
            UpdateChart();
            tbStatus.Text = $"{snapshot.Count} connections, {_allProcs.Count} apps"
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

    void RenderProcs()
    {
        if (_allProcs.Count == 0) { gridApps.ItemsSource = null; return; }
        var filter = txtSearch.Text.Trim();
        var list = filter.Length == 0
            ? _allProcs
            : _allProcs.Where(p => p.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || (p.Path?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        var selected = (gridApps.SelectedItem as ProcRow)?.Pid;
        gridApps.ItemsSource = list;
        if (selected is int pid) gridApps.SelectedItem = list.FirstOrDefault(p => p.Pid == pid);
    }

    void OpenInspector()
    {
        if (gridApps.SelectedItem is not ProcRow p) return;
        new InspectorWindow(p.Name, _rows.Where(r => r.Pid == p.Pid).ToList()) { Owner = this }.ShowDialog();
    }

    void Deny_Click(object sender, RoutedEventArgs e) => ToggleAppRule(sender, RuleAction.Deny);

    void Allow_Click(object sender, RoutedEventArgs e) => ToggleAppRule(sender, RuleAction.Allow);

    void ToggleAppRule(object sender, RuleAction action)
    {
        if (sender is not FrameworkElement { DataContext: ProcRow p }) return;
        var appRules = _store.All
            .Where(r => r.Server == null && r.Port == null && AppMatches(r, p.Path, p.Name))
            .ToList();
        var active = appRules.Any(r => r.Action == action);
        foreach (var rule in appRules) _store.Remove(rule);
        if (!active)
        {
            _store.Add(new Rule
            {
                Action = action,
                Lifetime = RuleLifetime.Forever,
                AppPath = p.Path,
                AppName = p.Path == null ? p.Name : null,
                Description = "quick toggle",
            });
            if (action == RuleAction.Deny)
                foreach (var c in _snapshot.Where(c => c.Pid == p.Pid)) EnforceDeny(c);
        }
        Tick();
    }

    static bool AppMatches(Rule rule, string? path, string name)
    {
        if (!string.IsNullOrEmpty(rule.AppPath))
            return path != null && string.Equals(rule.AppPath, path, StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(rule.AppName))
            return string.Equals(rule.AppName, name, StringComparison.OrdinalIgnoreCase);
        return false;
    }

    void UpdateSummary(List<Connection> snapshot, List<ProcRow> procs)
    {
        tbSummarySub.Text = $"{procs.Count} processes, {snapshot.Count} connections";
        tbSent.Text = Throughput.Bytes(_tp.TotalOut);
        tbRecv.Text = Throughput.Bytes(_tp.TotalIn);
        tbActive.Text = snapshot.Count.ToString();
        tbBlocked.Text = _blocked.Count.ToString();
        tbRules.Text = _store.All.Count().ToString();

        _topProcs.Clear();
        foreach (var p in procs.OrderByDescending(p => p.Connections).Take(5))
            _topProcs.Add(new SummaryItem { Name = p.Name, Detail = $"{p.Connections} conn", Icon = p.Icon });

        var domains = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var countries = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in snapshot)
        {
            _hosts.TryGetValue(c.Remote, out var host);
            var domain = DomainOf(host);
            if (domain != null) domains[domain] = domains.GetValueOrDefault(domain) + 1;
            var code = GeoIp.Lookup(c.Remote);
            if (code != null && code != "ZZ") countries[code] = countries.GetValueOrDefault(code) + 1;
        }

        _topDomains.Clear();
        foreach (var d in domains.OrderByDescending(d => d.Value).Take(5))
            _topDomains.Add(new SummaryItem { Name = d.Key, Detail = $"{d.Value} conn" });

        _topCountries.Clear();
        foreach (var d in countries.OrderByDescending(d => d.Value).Take(5))
            _topCountries.Add(new SummaryItem { Name = GeoData.NameOf(d.Key), Detail = $"{d.Value} conn" });
    }

    static string? DomainOf(string? host)
    {
        if (string.IsNullOrEmpty(host)) return null;
        if (IPAddress.TryParse(host, out _)) return null;
        var parts = host.TrimEnd('.').Split('.');
        return parts.Length >= 2 ? string.Join('.', parts[^2..]) : null;
    }

    void DrawArcs(List<Connection> snapshot)
    {
        arcLayer.Children.Clear();
        if (mapPath.Data == null) return;
        var start = new Point(500, 290);
        var accent = (Brush)FindResource("Accent");
        var danger = (Brush)FindResource("Danger");
        var muted = (Brush)FindResource("Muted");
        var border = (Brush)FindResource("BorderBrush");
        var surface = (Brush)FindResource("Surface2");
        var chips = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var c in snapshot.Take(120))
        {
            var code = GeoIp.Lookup(c.Remote);
            if (code == null || !GeoData.TryGet(code, out var country)) continue;
            var end = WorldMap.Project(country.Lat, country.Lon);
            _hosts.TryGetValue(c.Remote, out var host);
            var rule = RulesEngine.Match(_store.All, c.ProcessPath, c.ProcessName, c.Remote.ToString(), c.RemotePort, host);
            var brush = rule?.Action == RuleAction.Deny ? danger : rule?.Action == RuleAction.Allow ? accent : muted;
            AddArc(start, end, brush);
            if (chips.Count < 12 && chips.Add(code)) AddChip(code, end, border, surface);
        }

        var halo = new Ellipse { Width = 26, Height = 26, Fill = accent, Opacity = 0.15 };
        Canvas.SetLeft(halo, start.X - 13);
        Canvas.SetTop(halo, start.Y - 13);
        arcLayer.Children.Add(halo);
        var node = new Ellipse { Width = 10, Height = 10, Fill = accent, Stroke = (Brush)FindResource("Bg"), StrokeThickness = 2 };
        Canvas.SetLeft(node, start.X - 5);
        Canvas.SetTop(node, start.Y - 5);
        arcLayer.Children.Add(node);
        var label = new TextBlock { Text = "This PC", Foreground = (Brush)FindResource("Fg"), FontSize = 11, FontWeight = FontWeights.SemiBold };
        Canvas.SetLeft(label, start.X + 10);
        Canvas.SetTop(label, start.Y - 8);
        arcLayer.Children.Add(label);
    }

    void AddArc(Point start, Point end, Brush brush)
    {
        double dx = end.X - start.X, dy = end.Y - start.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1) return;
        var ctrl = new Point(
            (start.X + end.X) / 2 - dy * 0.18,
            (start.Y + end.Y) / 2 + dx * 0.18 - 40);
        var figure = new PathFigure { StartPoint = start };
        figure.Segments.Add(new QuadraticBezierSegment(ctrl, end, true));
        arcLayer.Children.Add(new Path
        {
            Data = new PathGeometry(new[] { figure }),
            Stroke = brush,
            StrokeThickness = 1.3,
            Opacity = 0.5,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
        });
        var dot = new Ellipse { Width = 5, Height = 5, Fill = brush };
        Canvas.SetLeft(dot, end.X - 2.5);
        Canvas.SetTop(dot, end.Y - 2.5);
        arcLayer.Children.Add(dot);
    }

    void AddChip(string code, Point at, Brush border, Brush surface)
    {
        var chip = new Border
        {
            Background = surface,
            BorderBrush = border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(4, 1, 4, 1),
            Child = new TextBlock { Text = code, FontSize = 10, Foreground = (Brush)FindResource("Fg") },
        };
        Canvas.SetLeft(chip, at.X + 6);
        Canvas.SetTop(chip, at.Y - 8);
        arcLayer.Children.Add(chip);
    }

    void UpdateChart()
    {
        _samples.Add((_tp.Input, _tp.Output));
        if (_samples.Count > 120) _samples.RemoveAt(0);
        tbChartRate.Text = $"down {Throughput.Bytes(_tp.Input)}/s   up {Throughput.Bytes(_tp.Output)}/s";
        chartCanvas.Children.Clear();
        double w = chartCanvas.ActualWidth, h = chartCanvas.ActualHeight;
        if (w < 10 || h < 10 || _samples.Count < 2) return;
        double max = Math.Max(1024, _samples.Max(s => Math.Max(s.Down, s.Up)));
        double scale = Math.Log10(1 + max);
        var down = new PointCollection();
        var up = new PointCollection();
        for (int i = 0; i < _samples.Count; i++)
        {
            double x = i / (double)(_samples.Count - 1) * w;
            down.Add(new Point(x, h - Math.Log10(1 + _samples[i].Down) / scale * h));
            up.Add(new Point(x, h - Math.Log10(1 + _samples[i].Up) / scale * h));
        }
        var fill = new PointCollection(down) { new Point(w, h), new Point(0, h) };
        chartCanvas.Children.Add(new Polygon { Points = fill, Fill = (Brush)FindResource("Accent"), Opacity = 0.16 });
        chartCanvas.Children.Add(new Polyline { Points = down, Stroke = (Brush)FindResource("Accent"), StrokeThickness = 1.4 });
        chartCanvas.Children.Add(new Polyline { Points = up, Stroke = new SolidColorBrush(Color.FromRgb(0x3D, 0xD5, 0x9B)), StrokeThickness = 1.2 });
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
        });
    }
}
