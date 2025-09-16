using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Untolia.Core;

public static class Assets
{
    public static Texture2D Player { get; private set; } = null!;

    public static void EnsurePlayerTexture(GraphicsDevice gd, ContentManager? content = null)
    {
        if (Player is not null && !Player.IsDisposed)
            return;

        if (content is not null)
            try
            {
                // Prefer the Caelen sprite under Content/Sprites/caelen.png
                Player = content.Load<Texture2D>("Sprites/caelen");
                return;
            }
            catch
            {
                // Fallbacks below
            }

        if (content is not null)
            try
            {
                // Legacy fallback if a generic 'player' asset exists
                Player = content.Load<Texture2D>("player");
                return;
            }
            catch
            {
                // Continue to placeholder fallback
            }

        Player = CreatePlayerPlaceholder(gd);
    }


    private static Texture2D CreatePlayerPlaceholder(GraphicsDevice gd)
    {
        const int w = 16, h = 24;
        var tex = new Texture2D(gd, w, h);
        var data = new Color[w * h];

        // Fill transparent background
        Array.Fill(data, Color.Transparent);

        void SetPixel(int x, int y, Color c)
        {
            data[y * w + x] = c;
        }

        // Body (blue rectangle)
        for (var y = 6; y < h; y++)
        for (var x = 2; x < w - 2; x++)
            SetPixel(x, y, new Color(40, 120, 200));

        // Head (skin-colored rectangle)
        for (var y = 0; y < 6; y++)
        for (var x = 3; x < w - 3; x++)
            SetPixel(x, y, new Color(240, 200, 160));

        // Eyes for orientation
        SetPixel(6, 2, Color.Black);
        SetPixel(9, 2, Color.Black);

        tex.SetData(data);
        return tex;
    }

    public static void Dispose()
    {
        Player?.Dispose();
    }
}