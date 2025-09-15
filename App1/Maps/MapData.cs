using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Untolia.Maps;

public sealed class MapData
{
    public string Id { get; init; } = "";
    public Point Size { get; init; }

    public Texture2D Bg { get; init; } = default!;
    public Texture2D Over { get; init; } = default!;
    public Texture2D CollisionMask { get; init; } = default!;
    public Texture2D? EncountersMask { get; init; }
    public Texture2D? LightingMask { get; init; }

    public EncountersConfig Encounters { get; init; } = new();
    public PuzzlesConfig? Puzzles { get; init; } // optional
    public PortalsConfig Portals { get; init; } = new();
    public NpcsConfig Npcs { get; init; } = new();
    public EventsConfig Events { get; init; } = new();
    public SettingsConfig Settings { get; init; } = new();

    public SpawnInfo Spawn { get; init; } = new();
    public Rectangle CameraBounds { get; init; }

    // Collision sampling data (built from CollisionMask at load)
    public bool[] CollisionBlocked { get; init; } = Array.Empty<bool>(); // true = blocked
    public int CollisionWidth { get; init; }
    public int CollisionHeight { get; init; }
}