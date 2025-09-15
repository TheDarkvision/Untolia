
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

namespace Untolia.Core.UI;

public sealed class InventoryMenu : Menu
{
    public InventoryMenu()
    {
        SetupMenu();
        PositionMenu();
    }

    private void SetupMenu()
    {
        // Placeholder inventory items
        AddMenuItem(new MenuItem("Healing Potion x3", UseHealingPotion));
        AddMenuItem(new MenuItem("Magic Sword", () => ShowItemInfo("Magic Sword", "A powerful enchanted blade")));
        AddMenuItem(new MenuItem("Iron Shield", () => ShowItemInfo("Iron Shield", "Provides good protection")));
        AddMenuItem(new MenuItem("Gold: 150", null)); // Non-selectable display item
        AddMenuItem(new MenuItem("Back", BackToGameMenu));
    }

    private void PositionMenu()
    {
        Size = new Vector2(Globals.ScreenSize.Y, 250);
        Position = new Vector2(
            (Globals.ScreenSize.X - Size.X) / 2f,
            (Globals.ScreenSize.Y - Size.Y) / 2f
        );
    }

    protected override void DrawTitle(SpriteBatch spriteBatch)
    {
        var title = "INVENTORY";
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

    private void UseHealingPotion()
    {
        var messageBox = new MessageBox(
            "Used Healing Potion!\nHP restored!",
            null,
            UIPosition.Center
        );
        Globals.UI.Add(messageBox);
    }

    private void ShowItemInfo(string itemName, string description)
    {
        var messageBox = new MessageBox(
            $"{itemName}\n\n{description}",
            null,
            UIPosition.Center
        );
        Globals.UI.Add(messageBox);
    }
}
