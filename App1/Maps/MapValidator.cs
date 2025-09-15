using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Untolia.Maps;

public static class MapValidator
{
    public static void ValidateDimensions(MapData d)
    {
        if (d.Size.X <= 0 || d.Size.Y <= 0)
            throw new InvalidDataException("Manifest size must be > 0.");

        EnsureSame(d.Bg, d.Size, "BG");
        EnsureSame(d.Over, d.Size, "Over");
        EnsureSame(d.CollisionMask, d.Size, "Collision");
        if (d.EncountersMask != null) EnsureSame(d.EncountersMask, d.Size, "Encounters");
        if (d.LightingMask != null) EnsureSame(d.LightingMask, d.Size, "Lighting");
    }

    private static void EnsureSame(Texture2D tex, Point size, string name)
    {
        if (tex.Width != size.X || tex.Height != size.Y)
            throw new InvalidDataException($"{name} size {tex.Width}x{tex.Height} != manifest size {size.X}x{size.Y}");
    }
}