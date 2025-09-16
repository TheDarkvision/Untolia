using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Untolia.Core.UI;

public enum UIPosition
{
    Center,
    Top,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public sealed class MessageBox : UIElement
{
    // Layout/style
    private const int MarginScreen = 24;
    private const int PaddingBox = 16;
    private const int HeaderHeight = 42;
    private const int FooterHeight = 32;
    private const int BorderThickness = 3;
    private readonly string _message;
    private readonly Action? _onClose;
    private readonly string _title;

    // Prevent immediate close from the same key press and rapid repeats
    private float _closeCooldown = 0.15f;
    private KeyboardState _previousKeyboard;

    // Cached wrapped text
    private string? _wrappedMessage;

    // Primary constructor with title
    public MessageBox(string title, string message, Action? onClose = null, UIPosition position = UIPosition.Center)
    {
        _title = string.IsNullOrWhiteSpace(title) ? "Message" : title;
        _message = message;
        _onClose = onClose;

        IsModal = true;
        CanReceiveFocus = true;

        CalculateSizeAndWrap();
        SetPosition(position);
    }

    // Back-compat constructor (no title)
    public MessageBox(string message, Action? onClose = null, UIPosition position = UIPosition.Center)
        : this("Message", message, onClose, position)
    {
    }

    public override void OnAdded()
    {
        _previousKeyboard = Keyboard.GetState();
        _closeCooldown = 0.15f;
    }

    private void CalculateSizeAndWrap()
    {
        // Max width ~ 60% of screen, min width 280
        var maxWidth = Math.Max(280f, Globals.ScreenSize.X * 0.6f);
        var contentMaxWidth = maxWidth - PaddingBox * 2 - BorderThickness * 2;

        // Wrap message to fit width
        _wrappedMessage = WrapText(UIAssets.DefaultFont, _message, contentMaxWidth);

        // Measure wrapped message height/width
        var msgSize = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, _wrappedMessage);

        // Compute final size including header/footer/padding/border
        var width = Math.Max(280f, msgSize.X + PaddingBox * 2 + BorderThickness * 2);
        var height = HeaderHeight + PaddingBox + msgSize.Y + PaddingBox + FooterHeight + BorderThickness * 2;

        // Cap height to 70% of screen; if overflow, re-wrap to reduced width/height
        var maxHeight = Globals.ScreenSize.Y * 0.7f;
        if (height > maxHeight)
        {
            // If too tall, reduce width slightly to allow more wrapping and reduce height
            contentMaxWidth = Math.Max(220f, contentMaxWidth * 0.9f);
            _wrappedMessage = WrapText(UIAssets.DefaultFont, _message, contentMaxWidth);
            msgSize = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, _wrappedMessage);
            width = Math.Max(280f, msgSize.X + PaddingBox * 2 + BorderThickness * 2);
            height = HeaderHeight + PaddingBox + msgSize.Y + PaddingBox + FooterHeight + BorderThickness * 2;
            height = Math.Min(height, maxHeight);
        }

        // Also cap width to 90% of screen
        var capWidth = Globals.ScreenSize.X * 0.9f;
        if (width > capWidth) width = capWidth;

        Size = new Vector2(width, height);
    }

    private void SetPosition(UIPosition position)
    {
        var screenWidth = Globals.ScreenSize.X;
        var screenHeight = Globals.ScreenSize.Y;
        var margin = MarginScreen;

        var pos = position switch
        {
            UIPosition.Center => new Vector2(
                (screenWidth - Size.X) / 2f,
                (screenHeight - Size.Y) / 2f),
            UIPosition.Top => new Vector2(
                (screenWidth - Size.X) / 2f,
                margin),
            UIPosition.Bottom => new Vector2(
                (screenWidth - Size.X) / 2f,
                screenHeight - Size.Y - margin),
            UIPosition.TopLeft => new Vector2(margin, margin),
            UIPosition.TopRight => new Vector2(
                screenWidth - Size.X - margin,
                margin),
            UIPosition.BottomLeft => new Vector2(
                margin,
                screenHeight - Size.Y - margin),
            UIPosition.BottomRight => new Vector2(
                screenWidth - Size.X - margin,
                screenHeight - Size.Y - margin),
            _ => new Vector2((screenWidth - Size.X) / 2f, (screenHeight - Size.Y) / 2f)
        };

        // Safety clamp: ensure panel stays fully on-screen
        var maxX = Math.Max(0f, screenWidth - Size.X - margin);
        var maxY = Math.Max(0f, screenHeight - Size.Y - margin);
        pos.X = MathHelper.Clamp(pos.X, margin, maxX);
        pos.Y = MathHelper.Clamp(pos.Y, margin, maxY);

        Position = pos;
    }


    public override void Update(float deltaTime)
    {
        if (_closeCooldown > 0f)
            _closeCooldown -= deltaTime;

        var keyboard = Keyboard.GetState();

        var closePressed =
            (!_previousKeyboard.IsKeyDown(Keys.Enter) && keyboard.IsKeyDown(Keys.Enter)) ||
            (!_previousKeyboard.IsKeyDown(Keys.Space) && keyboard.IsKeyDown(Keys.Space)) ||
            (!_previousKeyboard.IsKeyDown(Keys.Escape) && keyboard.IsKeyDown(Keys.Escape));

        if (_closeCooldown <= 0f && closePressed)
        {
            Globals.UI.Remove(this);
            _onClose?.Invoke();
            _closeCooldown = 0.15f;
        }

        _previousKeyboard = keyboard;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        // Dim overlay
        spriteBatch.Draw(UIAssets.PixelTexture, new Rectangle(0, 0, Globals.ScreenSize.X, Globals.ScreenSize.Y),
            new Color(0, 0, 0, 120));

        // Panel rects
        var panelRect = new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);
        var innerRect = new Rectangle(panelRect.X + BorderThickness, panelRect.Y + BorderThickness,
            panelRect.Width - BorderThickness * 2, panelRect.Height - BorderThickness * 2);
        var headerRect = new Rectangle(innerRect.X + 6, innerRect.Y + 6, innerRect.Width - 12, HeaderHeight - 12);
        var footerRect = new Rectangle(innerRect.X + 6, innerRect.Bottom - FooterHeight + 6, innerRect.Width - 12,
            FooterHeight - 12);

        // Background and border
        spriteBatch.Draw(UIAssets.PixelTexture, panelRect, Color.Black * 0.85f);
        DrawBorder(spriteBatch, panelRect, BorderThickness, new Color(255, 255, 255, 30));

        // Header
        spriteBatch.Draw(UIAssets.PixelTexture, headerRect, new Color(60, 60, 90, 160));
        var titleSize = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, _title);
        var titlePos = new Vector2(headerRect.X + 10, headerRect.Y + (headerRect.Height - titleSize.Y) / 2f);
        spriteBatch.DrawStringSafe(UIAssets.DefaultFont, _title, titlePos, Color.White);

        // Message
        var contentX = innerRect.X + PaddingBox;
        var contentY = headerRect.Bottom + PaddingBox;
        var contentW = innerRect.Width - PaddingBox * 2;
        var contentH = innerRect.Height - HeaderHeight - FooterHeight - PaddingBox * 2;
        var contentRect = new Rectangle(contentX, contentY, contentW, contentH);

        // Background tint for content
        spriteBatch.Draw(UIAssets.PixelTexture, contentRect, new Color(0, 0, 0, 40));

        // Draw wrapped text, clipping by content rect height (simple line-by-line)
        DrawWrappedText(spriteBatch, _wrappedMessage ?? _message, contentRect, Color.White);

        // Footer/instructions
        spriteBatch.Draw(UIAssets.PixelTexture, footerRect, new Color(30, 30, 30, 160));
        var instruction = "Enter/Space/Esc: Close";
        var instSize = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, instruction);
        var instPos = new Vector2(footerRect.X + (footerRect.Width - instSize.X) / 2f,
            footerRect.Y + (footerRect.Height - instSize.Y) / 2f);
        spriteBatch.DrawStringSafe(UIAssets.DefaultFont, instruction, instPos, Color.Gray);
    }

    private static void DrawBorder(SpriteBatch sb, Rectangle r, int thickness, Color c)
    {
        // Top/Bottom
        sb.Draw(UIAssets.PixelTexture, new Rectangle(r.X, r.Y, r.Width, thickness), c);
        sb.Draw(UIAssets.PixelTexture, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), c);
        // Left/Right
        sb.Draw(UIAssets.PixelTexture, new Rectangle(r.X, r.Y, thickness, r.Height), c);
        sb.Draw(UIAssets.PixelTexture, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), c);
    }

    private static string WrapText(SpriteFont font, string text, float maxLineWidth)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var words = text.Replace("\r", "").Split('\n');
        var sb = new StringBuilder();
        for (var i = 0; i < words.Length; i++)
        {
            // Each entry in 'words' is a line split by '\n'; wrap each line separately
            var line = words[i];
            sb.Append(WrapSingleLine(font, line, maxLineWidth));
            if (i < words.Length - 1) sb.Append('\n');
        }

        return sb.ToString();
    }

    private static string WrapSingleLine(SpriteFont font, string text, float maxLineWidth)
    {
        var words = text.Split(' ');
        var sb = new StringBuilder();
        var current = "";
        foreach (var w in words)
        {
            var test = string.IsNullOrEmpty(current) ? w : current + " " + w;
            var size = font.MeasureString(test);
            if (size.X <= maxLineWidth)
            {
                current = test;
            }
            else
            {
                if (!string.IsNullOrEmpty(current))
                {
                    sb.AppendLine(current);
                    current = w;
                }
                else
                {
                    // Single very long word; hard-break
                    sb.AppendLine(w);
                    current = "";
                }
            }
        }

        if (!string.IsNullOrEmpty(current))
            sb.Append(current);
        return sb.ToString();
    }

    private static void DrawWrappedText(SpriteBatch sb, string wrapped, Rectangle area, Color color)
    {
        var lines = wrapped.Replace("\r", "").Split('\n');
        float y = area.Y;
        foreach (var line in lines)
        {
            var sz = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, line);
            if (y + sz.Y > area.Bottom) break; // clip overflow
            sb.DrawStringSafe(UIAssets.DefaultFont, line, new Vector2(area.X, y), color);
            y += sz.Y;
        }
    }
}