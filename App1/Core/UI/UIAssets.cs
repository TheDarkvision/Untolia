using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Untolia.Core.UI;

public static class UIAssets
{
    public static Texture2D PixelTexture { get; private set; } = null!;
    public static SpriteFont DefaultFont { get; private set; } = null!;

    public static void Initialize(GraphicsDevice graphicsDevice, ContentManager content)
    {
        // Create 1x1 white pixel for UI backgrounds/borders
        PixelTexture = new Texture2D(graphicsDevice, 1, 1);
        PixelTexture.SetData(new[] { Color.White });

        // Try to load a font, create placeholder if none exists
        try
        {
            DefaultFont = content.Load<SpriteFont>("Fonts/Default");
        }
        catch
        {
            throw new InvalidOperationException("Please add a SpriteFont named 'Default' to Content/Fonts/");
        }
    }

    // Safe text rendering that replaces unsupported characters
    public static void DrawStringSafe(this SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 position,
        Color color)
    {
        var safeText = MakeFontSafe(text);
        spriteBatch.DrawString(font, safeText, position, color);
    }

    public static Vector2 MeasureStringSafe(SpriteFont font, string text)
    {
        var safeText = MakeFontSafe(text);
        return font.MeasureString(safeText);
    }

    private static string MakeFontSafe(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
            if (DefaultFont.Characters.Contains(c))
                sb.Append(c);
            else
                // Replace common special characters with ASCII equivalents
                sb.Append(c switch
                {
                    '▼' => 'v',
                    '▲' => '^',
                    '►' => '>',
                    '◄' => '<',
                    '•' => '*',
                    '…' => "...",
                    _ => '?' // Fallback for unknown characters
                });

        return sb.ToString();
    }

    public static void Dispose()
    {
        PixelTexture?.Dispose();
    }
}