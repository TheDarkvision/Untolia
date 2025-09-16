using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Untolia.Core.Maps;

public sealed class MapData
{
    public string Id { get; init; } = "";
    public Point Size { get; init; }

    public Texture2D Bg { get; init; } = default!;

    public Texture2D? Over { get; init; }


    // Optional raw masks if you want to keep them
    public Texture2D? EncountersMask { get; init; }
    public Texture2D? LightingMask { get; init; }

    // Gameplay configs (placeholders if you wire them later)
    // public EncountersConfig Encounters { get; init; } = new();
    // public PuzzlesConfig? Puzzles { get; init; }
    // public PortalsConfig Portals { get; init; } = new();
    // public NpcsConfig Npcs { get; init; } = new();
    // public EventsConfig Events { get; init; } = new();
    // public SettingsConfig Settings { get; init; } = new();

    public Point Spawn { get; init; } = new(0, 0);
    public Rectangle CameraBounds { get; init; }

    public IReadOnlyList<Portal> Portals { get; init; } = Array.Empty<Portal>();

    public IReadOnlyList<MapEvent> Events { get; init; } = Array.Empty<MapEvent>();


    // Collision sampling data
    // Always width * height in length, where width/height = CollisionWidth/CollisionHeight
    public bool[] CollisionBlocked { get; init; } = Array.Empty<bool>(); // true = blocked
    public int CollisionWidth { get; init; }
    public int CollisionHeight { get; init; }
}