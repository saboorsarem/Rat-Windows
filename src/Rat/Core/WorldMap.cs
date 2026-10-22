using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace Rat;

/// <summary>Builds the world silhouette from the bundled Natural Earth GeoJSON (equirectangular projection).</summary>
public static class WorldMap
{
    public const double Width = 1000;
    public const double Height = 500;

    public static Geometry? Build()
    {
        try
        {
            using var stream = OpenResource("world.geo.json");
            if (stream == null) return null;
            using var doc = JsonDocument.Parse(stream);
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                foreach (var feature in doc.RootElement.GetProperty("features").EnumerateArray())
                {
                    if (feature.TryGetProperty("properties", out var props)
                        && props.ValueKind == JsonValueKind.Object
                        && props.TryGetProperty("name", out var nm)
                        && nm.GetString() == "Antarctica") continue;
                    if (!feature.TryGetProperty("geometry", out var g) || g.ValueKind != JsonValueKind.Object) continue;
                    if (!g.TryGetProperty("type", out var t)) continue;
                    var type = t.GetString();
                    if (type == "Polygon") AddPolygon(ctx, g.GetProperty("coordinates"));
                    else if (type == "MultiPolygon")
                        foreach (var poly in g.GetProperty("coordinates").EnumerateArray())
                            AddPolygon(ctx, poly);
                }
            }
            geometry.Freeze();
            return geometry;
        }
        catch
        {
            return null;
        }
    }

    static void AddPolygon(StreamGeometryContext ctx, JsonElement rings)
    {
        foreach (var ring in rings.EnumerateArray())
        {
            bool first = true;
            foreach (var point in ring.EnumerateArray())
            {
                if (point.GetArrayLength() < 2) continue;
                var p = Project(point[1].GetDouble(), point[0].GetDouble());
                if (first)
                {
                    ctx.BeginFigure(p, true, true);
                    first = false;
                }
                else
                {
                    ctx.LineTo(p, true, false);
                }
            }
        }
    }

    public static Point Project(double lat, double lon)
        => new((lon + 180.0) / 360.0 * Width, (90.0 - lat) / 180.0 * Height);

    static Stream? OpenResource(string suffix)
    {
        var asm = typeof(WorldMap).Assembly;
        var name = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        return name == null ? null : asm.GetManifestResourceStream(name);
    }
}
