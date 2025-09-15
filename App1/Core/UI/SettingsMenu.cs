using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

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
        Size = new Vector2(280, 200);
        Position = new Vector2(
            (Globals.ScreenSize.X - Size.X) / 2f,
            (Globals.ScreenSize.Y - Size.Y) / 2f
        );
    }

    protected override void DrawTitle(SpriteBatch spriteBatch)
    {
        var title = "SETTINGS";
        var titleSize = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, title);
        var titlePos = new Vector2(
            Position.X + (Size.X - titleSize.X) / 2f,
            Position.Y + 20
        );
        spriteBatch.DrawStringSafe(UIAssets.DefaultFont, title, titlePos, Color.White);
    }

    protected override void CloseMenu()
    {
        BackToGameMenu();
    }

    private void BackToGameMenu()
    {
        Globals.UI.Remove(this);
        Globals.UI.Add(new GameMenu());
    }

    private void ShowAudioSettings()
    {
        var messageBox = new MessageBox(
            "Audio settings coming soon!",
            null,
            UIPosition.Center
        );
        Globals.UI.Add(messageBox);
    }

    private void ShowVideoSettings()
    {
        var messageBox = new MessageBox(
            "Video settings coming soon!",
            null,
            UIPosition.Center
        );
        Globals.UI.Add(messageBox);
    }

    private void ShowControls()
    {
        var messageBox = new MessageBox(
            "CONTROLS:\nWASD/Arrows: Move\nESC: Menu\nT: Test Message\nENTER/SPACE: Confirm",
            null,
            UIPosition.Center
        );
        Globals.UI.Add(messageBox);
    }
}
