using Xunit;

namespace Rat.Tests;

public class RulesEngineTests
{
    static Rule R(RuleAction action = RuleAction.Allow, string? app = null, string? server = null,
        ushort? port = null, bool enabled = true, RuleLifetime life = RuleLifetime.Forever)
        => new() { Enabled = enabled, Action = action, AppPath = app, Server = server, Port = port, Lifetime = life };

    const string App = @"C:\Program Files\Browser\chrome.exe";
    const string Server = "93.184.216.34";

    [Fact]
    public void MostSpecificRuleWins()
    {
        var rules = new[]
        {
            R(RuleAction.Allow, app: App),                // this app, any server (8)
            R(RuleAction.Deny, app: App, port: 443),      // this app, this port (10)
        };
        var m = RulesEngine.Match(rules, App, "chrome", Server, 443, null);
        Assert.NotNull(m);
        Assert.Equal(RuleAction.Deny, m.Action);
    }

    [Fact]
    public void DenyBeatsAllowOnTie()
    {
        var rules = new[]
        {
            R(RuleAction.Allow, server: Server),   // ip match (4) + port (2), app any
            R(RuleAction.Deny, server: Server),
        };
        var m = RulesEngine.Match(rules, App, "chrome", Server, 8443, null);
        Assert.NotNull(m);
        Assert.Equal(RuleAction.Deny, m.Action);
    }

    [Fact]
    public void DisabledAndOnceRulesAreIgnored()
    {
        var rules = new[]
        {
            R(RuleAction.Deny, app: App, enabled: false),
            R(RuleAction.Deny, server: Server, life: RuleLifetime.Once),
        };
        Assert.Null(RulesEngine.Match(rules, App, "chrome", "1.2.3.4", 80, null));
    }

    [Fact]
    public void ServerMatchesHostname()
    {
        var rules = new[] { R(RuleAction.Allow, server: "example.com") };
        Assert.NotNull(RulesEngine.Match(rules, App, "chrome", "93.184.216.34", 443, "example.com"));
        Assert.Null(RulesEngine.Match(rules, App, "chrome", "93.184.216.34", 443, "evil.com"));
    }

    [Fact]
    public void AppNameRuleMatchesProcessName()
    {
        var rules = new[] { new Rule { Action = RuleAction.Deny, AppName = "chrome" } };
        var m = RulesEngine.Match(rules, null, "chrome", Server, 443, null);
        Assert.NotNull(m);
        Assert.Equal(RuleAction.Deny, m.Action);
        Assert.Null(RulesEngine.Match(rules, null, "toknot", Server, 443, null));
    }

    [Fact]
    public void NoMatchReturnsNull()
    {
        var rules = new[] { R(RuleAction.Deny, app: @"C:\other.exe") };
        Assert.Null(RulesEngine.Match(rules, App, "chrome", Server, 443, null));
    }
}
