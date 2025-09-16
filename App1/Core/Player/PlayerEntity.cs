using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Untolia.Core.Player;

public sealed class PlayerEntity
{
    public Vector2 Position;
    public float Speed = 100f; // not used directly (controller uses its own constant)

    public PlayerEntity(Vector2 start, Point? size = null)
    {
        Position = start;
        Size = size ?? new Point(16, 16);
    }

    public Point Size { get; }

    public void Draw(SpriteBatch spriteBatch, Texture2D texture)
    {
        var tex = texture;
        var w = tex?.Width > 0 ? tex.Width : Size.X;
        var h = tex?.Height > 0 ? tex.Height : Size.Y;

        var dest = new Rectangle((int)(Position.X - w * 0.5f), (int)(Position.Y - h * 0.5f), w, h);
        spriteBatch.Draw(tex, dest, Color.White);
    }
}