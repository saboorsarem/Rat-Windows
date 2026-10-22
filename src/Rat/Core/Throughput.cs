using System.Net.NetworkInformation;

namespace Rat;

public sealed class Throughput
{
    readonly Dictionary<string, (long In, long Out, DateTime At)> _last = new();

    public double Input { get; private set; }
    public double Output { get; private set; }
    public string Text => $"down {Fmt(Input)}/s   up {Fmt(Output)}/s";

    public void Update()
    {
        double inBps = 0, outBps = 0;
        var now = DateTime.UtcNow;
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback || ni.OperationalStatus != OperationalStatus.Up) continue;
            try
            {
                var s = ni.GetIPStatistics();
                var prev = _last.TryGetValue(ni.Id, out var v)
                    ? v
                    : (In: s.BytesReceived, Out: s.BytesSent, At: now);
                var secs = (now - prev.At).TotalSeconds;
                if (secs > 0.5)
                {
                    inBps += Math.Max(0, s.BytesReceived - prev.In) / secs;
                    outBps += Math.Max(0, s.BytesSent - prev.Out) / secs;
                }
                _last[ni.Id] = (s.BytesReceived, s.BytesSent, now);
            }
            catch { }
        }
        Input = inBps;
        Output = outBps;
    }

    static string Fmt(double bps) => bps switch
    {
        >= 1_048_576 => $"{bps / 1_048_576:F1} MB",
        >= 1024 => $"{bps / 1024:F0} KB",
        _ => $"{bps:F0} B",
    };
}
