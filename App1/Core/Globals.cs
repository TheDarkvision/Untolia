using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Untolia.Core.Diagnostics;
using Untolia.Core.Inventory;
using Untolia.Core.RPG;
using Untolia.Core.UI;

namespace Untolia.Core;

public static class Globals
{
    public const int TileSize = 16;

    public static int GoldAmount = 0;

    public static GraphicsDevice GraphicsDevice = null!;
    public static SpriteBatch SpriteBatch = null!;
    public static GameTime GameTime = null!;
    public static Point ScreenSize = new(1440, 900);

    // Expose ContentManager so UI can load processed (xnb) content
    public static ContentManager Content = null!;

    // Logger (writes to Logs/untolia.log and echoes to console)
    public static readonly Logger Log = new(Path.Combine(AppContext.BaseDirectory, "Logs", "untolia.log"), true);

    // Simple game flags for events
    public static readonly HashSet<string> GameFlags = new();

    // Track triggered events across the game session (keys like "MapId:EventId")
    public static readonly HashSet<string> TriggeredEvents = new();

    // UI System
    public static UISystem UI { get; } = new();

    // Data registries
    public static CharacterRegistry Characters { get; } = new();

    // Global Party system
    public static PartyService Party { get; } = new();

    // Global Inventory
    public static InventoryService Inventory { get; } = new();
}