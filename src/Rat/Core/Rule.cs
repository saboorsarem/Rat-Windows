namespace Rat;

public enum RuleAction { Allow, Deny }

public enum RuleLifetime { Once, Session, Forever }

public sealed class Rule
{
    public bool Enabled { get; set; } = true;
    public RuleAction Action { get; set; }
    public RuleLifetime Lifetime { get; set; } = RuleLifetime.Forever;
    public string? AppPath { get; set; }   // null = any app
    public string? AppName { get; set; }   // fallback when the path is unknown; null = any app
    public string? Server { get; set; }    // ip or hostname; null = any server
    public ushort? Port { get; set; }      // null = any port
    public string? Description { get; set; }
}
