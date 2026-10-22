using System.Net;
using Xunit;

namespace Rat.Tests;

public class GeoIpTests
{
    [Fact]
    public void LookupReturnsCountryCode()
    {
        if (!GeoIp.Available) return; // the GeoIP database is downloaded by scripts/fetch-geoip.ps1
        var code = GeoIp.Lookup(IPAddress.Parse("8.8.8.8"));
        Assert.NotNull(code);
        Assert.Equal(2, code!.Length);
        Assert.True(GeoData.TryGet(code, out _));
    }

    [Fact]
    public void IPv6LookupReturnsNull()
    {
        if (!GeoIp.Available) return;
        Assert.Null(GeoIp.Lookup(IPAddress.IPv6Loopback));
    }
}
