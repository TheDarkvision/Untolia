// Camera2D.cs

using Microsoft.Xna.Framework;

namespace Untolia.Core;

public sealed class Camera2D
{
    private Vector2 _topLeft; // camera world-space top-left
    public float Zoom { get; set; } = 1f;
    public Rectangle Bounds { get; set; } = new(0, 0, 0, 0); // world/map bounds in pixels
    public Point ViewportSize { get; set; } = new(0, 0); // screen size in pixels

    public Matrix View { get; private set; } = Matrix.Identity;

    // Follow the target position (world-space), clamped to Bounds
    public void Follow(Vector2 targetWorldPos)
    {
        var vpW = (int)(ViewportSize.X / Zoom);
        var vpH = (int)(ViewportSize.Y / Zoom);

        // Desired top-left so that target is centered
        var desiredTopLeft = targetWorldPos - new Vector2(vpW, vpH) * 0.5f;

        // Clamp to world bounds
        float minX = Bounds.Left;
        float minY = Bounds.Top;
        float maxX = Bounds.Right - vpW;
        float maxY = Bounds.Bottom - vpH;

        // If the map is smaller than the viewport, center the map
        if (Bounds.Width <= vpW)
            desiredTopLeft.X = Bounds.Left + (Bounds.Width - vpW) * 0.5f;
        else
            desiredTopLeft.X = MathHelper.Clamp(desiredTopLeft.X, minX, maxX);

        if (Bounds.Height <= vpH)
            desiredTopLeft.Y = Bounds.Top + (Bounds.Height - vpH) * 0.5f;
        else
            desiredTopLeft.Y = MathHelper.Clamp(desiredTopLeft.Y, minY, maxY);

        // Pixel snapping to avoid subpixel jitter in pixel art
        _topLeft = new Vector2((float)Math.Floor(desiredTopLeft.X), (float)Math.Floor(desiredTopLeft.Y));

        // Build view matrix: translate world so top-left aligns with screen origin, then apply zoom
        View =
            Matrix.CreateTranslation(-_topLeft.X, -_topLeft.Y, 0f) *
            Matrix.CreateScale(Zoom, Zoom, 1f);
    }
}