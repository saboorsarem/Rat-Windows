using System.Diagnostics;
using System.Net;
using System.Security.Principal;

namespace Rat;

public static class Blocker
{
    public static bool IsAdmin()
    {
        using var id = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>Adds an outbound Windows Firewall block rule for one server address. Same honest limit as the macOS pf table: the address is blocked for every app.</summary>
    public static void Block(IPAddress ip)
    {
        var name = $"Rat block {ip}";
        RunNetsh($"advfirewall firewall delete rule name=\"{name}\"");
        RunNetsh($"advfirewall firewall add rule name=\"{name}\" dir=out action=block remoteip={ip} group=\"Rat\"");
    }

    public static void Unblock(IPAddress ip) => RunNetsh($"advfirewall firewall delete rule name=\"Rat block {ip}\"");

    static void RunNetsh(string args)
    {
        try
        {
            var psi = new ProcessStartInfo("netsh.exe", args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var p = Process.Start(psi)!;
            p.WaitForExit(10_000);
        }
        catch { }
    }
}
