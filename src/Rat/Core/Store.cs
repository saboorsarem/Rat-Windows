using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rat;

public sealed class Store
{
    static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Rat");
    static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public List<Rule> Forever { get; set; } = new();
    public List<Rule> Session { get; } = new();
    public bool AlertMode { get; set; }

    public IEnumerable<Rule> All => Session.Concat(Forever);

    public static Store Load()
    {
        var s = new Store();
        try
        {
            if (File.Exists(Path.Combine(Dir, "rules.json")))
                s.Forever = JsonSerializer.Deserialize<List<Rule>>(File.ReadAllText(Path.Combine(Dir, "rules.json")), Json) ?? new();
            if (File.Exists(Path.Combine(Dir, "settings.json")))
            {
                var e = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(Path.Combine(Dir, "settings.json")));
                s.AlertMode = e.ValueKind == JsonValueKind.Object
                    && e.TryGetProperty("alertMode", out var m)
                    && m.ValueKind == JsonValueKind.True;
            }
        }
        catch { }
        return s;
    }

    public void SaveRules() => Safe(() => File.WriteAllText(Path.Combine(Dir, "rules.json"), JsonSerializer.Serialize(Forever, Json)));
    public void SaveSettings() => Safe(() => File.WriteAllText(Path.Combine(Dir, "settings.json"), JsonSerializer.Serialize(new { alertMode = AlertMode }, Json)));

    public void Add(Rule rule)
    {
        if (rule.Lifetime == RuleLifetime.Forever) { Forever.Add(rule); SaveRules(); }
        else if (rule.Lifetime == RuleLifetime.Session) Session.Add(rule);
    }

    public void Remove(Rule rule)
    {
        if (Forever.Remove(rule)) SaveRules();
        Session.Remove(rule);
    }

    public void ReplaceAll(List<Rule> rules)
    {
        Forever = rules.Where(r => r.Lifetime != RuleLifetime.Session).ToList();
        SaveRules();
    }

    static void Safe(Action a)
    {
        try { Directory.CreateDirectory(Dir); a(); }
        catch { }
    }
}
