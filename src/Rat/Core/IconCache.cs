using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rat;

public static class IconCache
{
    static readonly Dictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static ImageSource? Get(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (Cache.TryGetValue(path, out var cached)) return cached;
        ImageSource? source = null;
        try
        {
            using var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
            if (icon != null)
            {
                source = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
            }
        }
        catch { }
        Cache[path] = source;
        return source;
    }
}
