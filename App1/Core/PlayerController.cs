using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Untolia.Maps;

namespace Untolia.Core;

public sealed class PlayerController
{
    public float SpeedPixelsPerSecond { get; set; } = 150f;

    // Movement with collision against a blocked mask.
    public void Update(ref Vector2 position, float dt, Point mapPixelSize, Texture2D playerTexture,
        bool[] blocked, int blockedW, int blockedH)
    {
        var ks = Keyboard.GetState();

        float dx = 0f, dy = 0f;
        if (ks.IsKeyDown(Keys.Left) || ks.IsKeyDown(Keys.A)) dx -= 1f;
        if (ks.IsKeyDown(Keys.Right) || ks.IsKeyDown(Keys.D)) dx += 1f;
        if (ks.IsKeyDown(Keys.Up) || ks.IsKeyDown(Keys.W)) dy -= 1f;
        if (ks.IsKeyDown(Keys.Down) || ks.IsKeyDown(Keys.S)) dy += 1f;

        var move = new Vector2(dx, dy);
        if (move == Vector2.Zero)
            return;

        move.Normalize();
        var desired = move * SpeedPixelsPerSecond * dt;

        // Axis-wise move and collide
        var rect = new Rectangle((int)position.X, (int)position.Y, playerTexture.Width, playerTexture.Height);

        // Move X
        var nextRectX = rect;
        nextRectX.X = (int)(position.X + desired.X);
        nextRectX.X = MathHelper.Clamp(nextRectX.X, 0, mapPixelSize.X - nextRectX.Width);
        if (!CollisionMaskHelper.RectBlocked(blocked, blockedW, blockedH, nextRectX))
        {
            position.X = nextRectX.X;
            rect = nextRectX;
        }

        // Move Y
        var nextRectY = rect;
        nextRectY.Y = (int)(position.Y + desired.Y);
        nextRectY.Y = MathHelper.Clamp(nextRectY.Y, 0, mapPixelSize.Y - nextRectY.Height);
        if (!CollisionMaskHelper.RectBlocked(blocked, blockedW, blockedH, nextRectY)) position.Y = nextRectY.Y;
    }
}