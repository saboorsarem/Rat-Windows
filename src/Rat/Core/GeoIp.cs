using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;

namespace Rat;

/// <summary>Offline country lookup over the embedded DB-IP Lite database (CC BY 4.0).</summary>
public static class GeoIp
{
    readonly record struct Range(uint Start, uint End, string Code);

    static readonly List<Range> Ranges = new();

    public static bool Available { get; private set; }

    static GeoIp() => Load();

    static void Load()
    {
        try
        {
            var asm = typeof(GeoIp).Assembly;
            var name = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("geoip-country.csv.gz", StringComparison.OrdinalIgnoreCase));
            if (name == null) return;
            using var raw = asm.GetManifestResourceStream(name)!;
            using var gz = new GZipStream(raw, CompressionMode.Decompress);
            using var reader = new StreamReader(gz);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split(',');
                if (parts.Length < 3) continue;
                if (!TryParseIp(parts[0].Trim('"'), out var start)) continue;
                if (!TryParseIp(parts[1].Trim('"'), out var end)) continue;
                var code = parts[2].Trim('"');
                if (code.Length == 0) continue;
                Ranges.Add(new Range(start, end, code));
            }
            Available = Ranges.Count > 0;
        }
        catch
        {
            Ranges.Clear();
            Available = false;
        }
    }

    static bool TryParseIp(string text, out uint value)
    {
        value = 0;
        var parts = text.Split('.');
        if (parts.Length != 4) return false;
        uint v = 0;
        foreach (var part in parts)
        {
            if (!byte.TryParse(part, out var b)) return false;
            v = (v << 8) | b;
        }
        value = v;
        return true;
    }

    public static string? Lookup(IPAddress ip)
    {
        if (!Available || ip.AddressFamily != AddressFamily.InterNetwork) return null;
        var b = ip.GetAddressBytes();
        uint v = ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
        int lo = 0, hi = Ranges.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;
            var range = Ranges[mid];
            if (v < range.Start) hi = mid - 1;
            else if (v > range.End) lo = mid + 1;
            else return range.Code;
        }
        return null;
    }
}
