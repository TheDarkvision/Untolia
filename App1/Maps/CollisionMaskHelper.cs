using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Untolia.Maps;

public static class CollisionMaskHelper
{
    // Converts a collision mask into a blocked array: true = blocked, false = walkable.
    // Convention: Transparent = walkable; Opaque near-black = blocked.
    public static bool[] BuildBlocked(Texture2D mask, out int width, out int height)
    {
        width = mask.Width;
        height = mask.Height;

        var pixels = new Color[width * height];
        mask.GetData(pixels);

        var blocked = new bool[pixels.Length];
        for (var i = 0; i < pixels.Length; i++)
        {
            var c = pixels[i];
            // Transparent means walkable
            if (c.A == 0)
            {
                blocked[i] = false;
                continue;
            }

            // Treat near-black as blocked
            var isBlack = c.R < 8 && c.G < 8 && c.B < 8;
            blocked[i] = isBlack;
        }

        return blocked;
    }

    public static bool RectBlocked(bool[] blocked, int width, int height, Rectangle rect)
    {
        // Clamp rect to image bounds
        var left = Math.Clamp(rect.Left, 0, width - 1);
        var right = Math.Clamp(rect.Right - 1, 0, width - 1);
        var top = Math.Clamp(rect.Top, 0, height - 1);
        var bottom = Math.Clamp(rect.Bottom - 1, 0, height - 1);

        for (var y = top; y <= bottom; y++)
        {
            var row = y * width;
            for (var x = left; x <= right; x++)
                if (blocked[row + x])
                    return true;
        }

        return false;
    }
}