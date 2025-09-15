using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Untolia.Core.UI;

namespace Untolia.Core;

public static class Globals
{
    public const int TileSize = 16;

    public static GraphicsDevice GraphicsDevice = null!;
    public static SpriteBatch SpriteBatch = null!;
    public static GameTime GameTime = null!;
    public static Point ScreenSize = new(1440, 900);

    // UI System
    public static UISystem UI { get; } = new();
}