using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Untolia.Core.UI;

public abstract class UIElement
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsModal { get; set; } = false;
    public bool CanReceiveFocus { get; set; } = false;
    public Color BackgroundColor { get; set; } = Color.Black * 0.8f;
    public Color BorderColor { get; set; } = Color.White;
    public int BorderThickness { get; set; } = 2;

    public Rectangle Bounds => new((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);

    // Allow elements to prime their input state when added to UI
    public virtual void OnAdded() { }

    public virtual void Update(float deltaTime) { }
    public abstract void Draw(SpriteBatch spriteBatch);

    protected void DrawBackground(SpriteBatch spriteBatch)
    {
        var bounds = Bounds;
        
        // Background
        spriteBatch.Draw(UIAssets.PixelTexture, bounds, BackgroundColor);
        
        // Border
        if (BorderThickness > 0)
        {
            var t = BorderThickness;
            spriteBatch.Draw(UIAssets.PixelTexture, new Rectangle(bounds.X, bounds.Y, bounds.Width, t), BorderColor);
            spriteBatch.Draw(UIAssets.PixelTexture, new Rectangle(bounds.X, bounds.Bottom - t, bounds.Width, t), BorderColor);
            spriteBatch.Draw(UIAssets.PixelTexture, new Rectangle(bounds.X, bounds.Y, t, bounds.Height), BorderColor);
            spriteBatch.Draw(UIAssets.PixelTexture, new Rectangle(bounds.Right - t, bounds.Y, t, bounds.Height), BorderColor);
        }
    }
}