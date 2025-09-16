using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Untolia.Core.Player;

public sealed class PlayerController
{
    // Moves the player with WASD/Arrow keys, clamps to map bounds, and resolves collisions
    public void Update(
        ref Vector2 position,
        float dt,
        Point mapPixelSize,
        Texture2D playerTexture,
        bool[] blocked,
        int blockedW,
        int blockedH)
    {
        // Input axes
        var dir = new Vector2(Input.AxisX(), Input.AxisY());
        if (dir != Vector2.Zero) dir.Normalize();

        // Speed (pixels/second)
        const float speed = 100f;
        var desired = position + dir * speed * dt;

        // Player rect (use texture bounds if available; fallback to 16x16)
        int pw = playerTexture?.Width > 0 ? playerTexture.Width : 16;
        int ph = playerTexture?.Height > 0 ? playerTexture.Height : 16;

        // Clamp to map bounds
        desired.X = MathHelper.Clamp(desired.X, pw * 0.5f, mapPixelSize.X - pw * 0.5f);
        desired.Y = MathHelper.Clamp(desired.Y, ph * 0.5f, mapPixelSize.Y - ph * 0.5f);

        // Collision resolve (simple: try X then Y)
        var currentRect = new Rectangle((int)(position.X - pw * 0.5f), (int)(position.Y - ph * 0.5f), pw, ph);
        var desiredRect = new Rectangle((int)(desired.X - pw * 0.5f), (int)(position.Y - ph * 0.5f), pw, ph);

        if (!RectBlocked(blocked, blockedW, blockedH, desiredRect))
        {
            position.X = desired.X;
        }

        desiredRect = new Rectangle((int)(position.X - pw * 0.5f), (int)(desired.Y - ph * 0.5f), pw, ph);
        if (!RectBlocked(blocked, blockedW, blockedH, desiredRect))
        {
            position.Y = desired.Y;
        }
    }

    private static bool RectBlocked(bool[] blocked, int w, int h, Rectangle rect)
    {
        if (blocked.Length == 0 || w <= 0 || h <= 0) return false;

        // Clamp rect to mask bounds to avoid OOB
        int left = Math.Max(0, rect.Left);
        int right = Math.Min(w - 1, rect.Right - 1);
        int top = Math.Max(0, rect.Top);
        int bottom = Math.Min(h - 1, rect.Bottom - 1);

        for (int y = top; y <= bottom; y++)
        {
            int row = y * w;
            for (int x = left; x <= right; x++)
            {
                if (blocked[row + x]) return true;
            }
        }
        return false;
    }
}
