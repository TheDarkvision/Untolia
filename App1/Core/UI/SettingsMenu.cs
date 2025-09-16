using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Untolia.Core.UI;

public sealed class SettingsMenu : Menu
{
    public SettingsMenu()
    {
        SetupMenu();
        PositionMenu();
    }

    private void SetupMenu()
    {
        AddMenuItem(new MenuItem("Audio Settings", ShowAudioSettings));
        AddMenuItem(new MenuItem("Video Settings", ShowVideoSettings));
        AddMenuItem(new MenuItem("Controls", ShowControls));
        AddMenuItem(new MenuItem("Back", BackToGameMenu));
    }

    private void PositionMenu()
    {
        // Left-side, full height panel with a nice margin (same style as GameMenu)
        var margin = 24f;
        var panelWidth = (int)Math.Clamp(Globals.ScreenSize.X * 0.28f, 280f, 420f);
        var panelHeight = Globals.ScreenSize.Y - (int)(margin * 2);
        Size = new Vector2(panelWidth, panelHeight);
        Position = new Vector2(margin, margin);
    }

    protected override void CloseMenu()
    {
        BackToGameMenu();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        // Panel background
        var panelRect = new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);
        spriteBatch.Draw(UIAssets.PixelTexture, panelRect, Color.Black * 0.80f);

        // Border
        DrawBorder(spriteBatch, panelRect, 3, new Color(255, 255, 255, 30));

        // Header
        var headerHeight = 48;
        var headerRect = new Rectangle(panelRect.X + 6, panelRect.Y + 6, panelRect.Width - 12, headerHeight);
        spriteBatch.Draw(UIAssets.PixelTexture, headerRect, new Color(60, 60, 90, 160));
        var title = "SETTINGS";
        var titleSize = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, title);
        var titlePos = new Vector2(headerRect.X + 12, headerRect.Y + (headerRect.Height - titleSize.Y) / 2f);
        spriteBatch.DrawStringSafe(UIAssets.DefaultFont, title, titlePos, Color.White);

        // Items
        float y = headerRect.Bottom + 16;
        var lineHeight = 36f;
        float leftPadding = panelRect.X + 24;
        float rightPadding = panelRect.Right - 24;

        for (var i = 0; i < _items.Count; i++)
        {
            var isSelected = i == _selectedIndex;
            var item = _items[i];

            // Separator before "Back"
            if (i == _items.Count - 1)
                DrawSeparator(spriteBatch, leftPadding, rightPadding, y - 8);

            var itemText = item.Text;
            var textSize = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, itemText);

            if (isSelected)
            {
                var highlightRect = new Rectangle(panelRect.X + 6, (int)(y - 6), panelRect.Width - 12, (int)lineHeight);
                spriteBatch.Draw(UIAssets.PixelTexture, highlightRect, new Color(90, 90, 120, 160));
                var accent = new Rectangle(panelRect.X + 6, (int)(y - 6), 4, (int)lineHeight);
                spriteBatch.Draw(UIAssets.PixelTexture, accent, Color.CornflowerBlue);
            }

            var color = isSelected ? Color.White : new Color(220, 220, 220);
            var pos = new Vector2(leftPadding, y + (lineHeight - textSize.Y) / 2f - 4f);
            spriteBatch.DrawStringSafe(UIAssets.DefaultFont, itemText, pos, color);

            y += lineHeight + 4f;
        }

        // Bottom hint bar
        var hintText = "Enter: Select    Esc: Back    ↑/↓: Navigate";
        var hintSize = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, hintText);
        var hintRect = new Rectangle(panelRect.X + 6, panelRect.Bottom - 40, panelRect.Width - 12, 34);
        spriteBatch.Draw(UIAssets.PixelTexture, hintRect, new Color(30, 30, 30, 160));
        var hintPos = new Vector2(panelRect.X + (panelRect.Width - hintSize.X) / 2f,
            hintRect.Y + (hintRect.Height - hintSize.Y) / 2f);
        spriteBatch.DrawStringSafe(UIAssets.DefaultFont, hintText, hintPos, Color.Gray);
    }

    private static void DrawBorder(SpriteBatch sb, Rectangle r, int thickness, Color c)
    {
        sb.Draw(UIAssets.PixelTexture, new Rectangle(r.X, r.Y, r.Width, thickness), c);
        sb.Draw(UIAssets.PixelTexture, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), c);
        sb.Draw(UIAssets.PixelTexture, new Rectangle(r.X, r.Y, thickness, r.Height), c);
        sb.Draw(UIAssets.PixelTexture, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), c);
    }

    private static void DrawSeparator(SpriteBatch sb, float left, float right, float y)
    {
        var rect = new Rectangle((int)left, (int)y, (int)(right - left), 1);
        sb.Draw(UIAssets.PixelTexture, rect, new Color(255, 255, 255, 30));
    }

    private void BackToGameMenu()
    {
        Globals.UI.Remove(this);
        Globals.UI.Add(new GameMenu());
    }

    private void ShowAudioSettings()
    {
        var messageBox = new MessageBox(
            "Audio settings coming soon!"
        );
        Globals.UI.Add(messageBox);
    }

    private void ShowVideoSettings()
    {
        var messageBox = new MessageBox(
            "Video settings coming soon!"
        );
        Globals.UI.Add(messageBox);
    }

    private void ShowControls()
    {
        var messageBox = new MessageBox(
            "CONTROLS:\nWASD/Arrows: Move\nESC: Menu\nT: Test Message\nENTER/SPACE: Confirm"
        );
        Globals.UI.Add(messageBox);
    }
}