using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Untolia.Maps;

public static class EncounterMaskSampler
{
    // Samples a pixel from the encounters mask; caller maps it using EncounterZoneResolver.
    // x,y are in image pixel space.
    public static Color Sample(Texture2D mask, int x, int y)
    {
        if (x < 0 || y < 0 || x >= mask.Width || y >= mask.Height)
            return new Color(0, 0, 0, 0);

        // Efficient 1-pixel GetData
        var rect = new Rectangle(x, y, 1, 1);
        var data = new Color[1];
        mask.GetData(0, rect, data, 0, 1);
        return data[0];
    }
}