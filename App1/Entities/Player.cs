using Microsoft.Xna.Framework;
using Untolia.Core;

namespace Untolia.Entities;

public class Player
{
    private readonly Point _size = new(16, 16);
    public Vector2 Pos;
    public float Speed = 80f; // pixels/sec

    public Player(Vector2 start)
    {
        Pos = start;
    }

    // Expose size so scenes can clamp by half-size
    public Point Size => _size;

    public void Update(float dt)
    {
        // Screen/world coordinates both have Y increasing downward here,
        // so no extra negation is needed.
        var dir = new Vector2(Input.AxisX(), Input.AxisY());
        if (dir != Vector2.Zero) dir.Normalize();
        Pos += dir * Speed * dt;
    }

    public void Draw()
    {
        // Draw using player.png if available, else fallback to a 1x1 dot
        var tex = Assets.Player;
        Globals.SpriteBatch.Draw(tex,
            new Rectangle((int)Pos.X - _size.X / 2, (int)Pos.Y - _size.Y / 2, _size.X, _size.Y), Color.White);
    }
}