using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

namespace Untolia.Core.UI;

public sealed class GameMenu : Menu
{
    public GameMenu()
    {
        SetupMenu();
        PositionMenu();
    }

    private void SetupMenu()
    {
        AddMenuItem(new MenuItem("Resume", CloseMenu));
        AddMenuItem(new MenuItem("Inventory", ShowInventory));
        AddMenuItem(new MenuItem("Settings", ShowSettings));
        AddMenuItem(new MenuItem("Save Game", SaveGame));
        AddMenuItem(new MenuItem("Load Game", LoadGame));
        AddMenuItem(new MenuItem("Exit to Main Menu", ExitToMainMenu));
    }

    private void PositionMenu()
    {
        Size = new Vector2(300, 300);
        var margin = 40f;
        Position = new Vector2(
            Globals.ScreenSize.X - Size.X - margin, // right-aligned
            (Globals.ScreenSize.Y - Size.Y) / 2f    // vertically centered
        );
    }

    protected override void DrawTitle(SpriteBatch spriteBatch)
    {
        var title = "GAME MENU";
        var titleSize = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, title);
        var titlePos = new Vector2(
            Position.X + (Size.X - titleSize.X) / 2f,
            Position.Y + 20
        );
        spriteBatch.DrawStringSafe(UIAssets.DefaultFont, title, titlePos, Color.White);
    }

    protected override void CloseMenu()
    {
        Globals.UI.Remove(this);
    }

    private void ShowInventory()
    {
        Globals.UI.Remove(this);
        Globals.UI.Add(new InventoryMenu());
    }

    private void ShowSettings()
    {
        Globals.UI.Remove(this);
        Globals.UI.Add(new SettingsMenu());
    }

    private void SaveGame()
    {
        var messageBox = new MessageBox(
            "Game saved successfully!",
            null,
            UIPosition.Center
        );
        Globals.UI.Add(messageBox);
    }

    private void LoadGame()
    {
        var messageBox = new MessageBox(
            "Load game functionality not implemented yet.",
            null,
            UIPosition.Center
        );
        Globals.UI.Add(messageBox);
    }

    private void ExitToMainMenu()
    {
        var messageBox = new MessageBox(
            "Exit to main menu functionality not implemented yet.",
            null,
            UIPosition.Center
        );
        Globals.UI.Add(messageBox);
    }
}
