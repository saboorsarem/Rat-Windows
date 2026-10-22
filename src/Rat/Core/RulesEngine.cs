namespace Rat;

public static class RulesEngine
{
    /// <summary>Most specific rule wins; deny beats allow on a tie. Once-lifetime rules are decisions, not matchers.</summary>
    public static Rule? Match(IEnumerable<Rule> rules, string? appPath, string? processName, string server, ushort port, string? host)
    {
        Rule? best = null;
        int bestScore = int.MinValue;
        foreach (var r in rules)
        {
            if (!r.Enabled || r.Lifetime == RuleLifetime.Once) continue;
            if (!Matches(r, appPath, processName, server, port, host, out var score)) continue;
            bool better = best == null || score > bestScore
                || (score == bestScore && r.Action == RuleAction.Deny && best.Action != RuleAction.Deny);
            if (better) { best = r; bestScore = score; }
        }
        return best;
    }

    static bool Matches(Rule r, string? appPath, string? processName, string server, ushort port, string? host, out int score)
    {
        score = 0;
        if (!string.IsNullOrEmpty(r.AppPath))
        {
            if (appPath is null || !string.Equals(appPath, r.AppPath, StringComparison.OrdinalIgnoreCase)) return false;
            score += 8;
        }
        else if (!string.IsNullOrEmpty(r.AppName))
        {
            if (string.IsNullOrEmpty(processName) || !string.Equals(processName, r.AppName, StringComparison.OrdinalIgnoreCase)) return false;
            score += 6;
        }
        if (!string.IsNullOrEmpty(r.Server))
        {
            bool byIp = string.Equals(server, r.Server, StringComparison.OrdinalIgnoreCase);
            bool byHost = host != null && string.Equals(host, r.Server, StringComparison.OrdinalIgnoreCase);
            if (!byIp && !byHost) return false;
            score += byHost ? 5 : 4;
        }
        if (r.Port is ushort p && p != port) return false;
        if (r.Port is not null) score += 2;
        return true;
    }
}
