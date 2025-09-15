using Microsoft.Xna.Framework;

namespace Untolia.Maps;

public static class EncounterZoneResolver
{
    // Map hex string to Color and group id for exact match lookups.
    public static Dictionary<Color, string> BuildColorMap(EncountersConfig cfg)
    {
        var dict = new Dictionary<Color, string>(new ColorEqualityComparer());
        foreach (var kv in cfg.ZoneColors)
        {
            var color = HexToColor(kv.Key);
            dict[color] = kv.Value;
        }

        return dict;
    }


    public static string? GetGroupForColor(Color pixel, Dictionary<Color, string> colorMap)
    {
        return colorMap.TryGetValue(new Color(pixel.R, pixel.G, pixel.B, (byte)255), out var group) ? group : null;
    }


    // Supports "#RRGGBB" or "RRGGBB"
    public static Color HexToColor(string hex)
    {
        hex = hex.Trim();
        if (hex.StartsWith('#')) hex = hex[1..];
        if (hex.Length != 6) throw new ArgumentException("Expected hex color in RRGGBB format.");
        var r = Convert.ToByte(hex[..2], 16);
        var g = Convert.ToByte(hex[2..4], 16);
        var b = Convert.ToByte(hex[4..6], 16);
        return new Color(r, g, b, (byte)255);
    }


    private sealed class ColorEqualityComparer : IEqualityComparer<Color>
    {
        public bool Equals(Color x, Color y)
        {
            return x.R == y.R && x.G == y.G && x.B == y.B;
        }

        public int GetHashCode(Color obj)
        {
            return (obj.R << 16) | (obj.G << 8) | obj.B;
        }
    }
}