using System.Net.NetworkInformation;

namespace Rat;

public sealed class Throughput
{
    readonly Dictionary<string, (long In, long Out, DateTime At)> _last = new();

    public double Input { get; private set; }
    public double Output { get; private set; }
    public double TotalIn { get; private set; }
    public double TotalOut { get; private set; }
    public string Text => $"down {Bytes(Input)}/s   up {Bytes(Output)}/s";

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
                var deltaIn = s.BytesReceived - prev.In;
                var deltaOut = s.BytesSent - prev.Out;
                var secs = (now - prev.At).TotalSeconds;
                if (secs > 0.5)
                {
                    inBps += Math.Max(0, deltaIn) / secs;
                    outBps += Math.Max(0, deltaOut) / secs;
                }
                if (deltaIn > 0) TotalIn += deltaIn;
                if (deltaOut > 0) TotalOut += deltaOut;
                _last[ni.Id] = (s.BytesReceived, s.BytesSent, now);
            }
            catch { }
        }
        Input = inBps;
        Output = outBps;
    }

    public static string Bytes(double bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824:F1} GB",
        >= 1_048_576 => $"{bytes / 1_048_576:F1} MB",
        >= 1024 => $"{bytes / 1024:F0} KB",
        _ => $"{bytes:F0} B",
    };
}
