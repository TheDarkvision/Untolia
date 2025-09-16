namespace Untolia.Core.Maps;

public sealed class MapManifest
{
    public string Id { get; set; } = "";
    public Size2? Size { get; set; } = new() { Width = Globals.ScreenSize.X, Height = Globals.ScreenSize.Y };
    public int TileSize { get; set; } = 16;

    public LayersSection? Layers { get; set; }
    public MasksSection? Masks { get; set; }

    // Optional sections (wire as needed)
    public List<NpcDef>? Npcs { get; set; }
    public List<PortalDef>? Portals { get; set; }
    public List<EventDef>? Events { get; set; }
    public EncountersSection? Encounters { get; set; }
    public LightingSection? Lighting { get; set; }

    public Point2? PlayerSpawn { get; set; } = new() { X = 0, Y = 0 };
}

public sealed class Size2
{
    public int Width { get; set; }
    public int Height { get; set; }
}

public sealed class Point2
{
    public int X { get; set; }
    public int Y { get; set; }
}

public sealed class LayersSection
{
    public string? Background { get; set; }
    public string? Overhead { get; set; }
}

public sealed class MasksSection
{
    public string? Collision { get; set; }
    public string? Encounters { get; set; }
    public string? Lighting { get; set; }
}

public sealed class NpcDef
{
    public string Id { get; set; } = "";
    public string Texture { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public string? DialogueKey { get; set; }
}

public sealed class PortalDef
{
    public string Id { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public string TargetMap { get; set; } = "";
    public Point2 TargetSpawn { get; set; } = new();
}

public sealed class EventDef
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string When { get; set; } = "manual";
    public string? Flag { get; set; }
    public bool? Value { get; set; }
}

public sealed class EncountersSection
{
    public bool Enabled { get; set; } = false;
    public double Rate { get; set; } = 0.03;
    public List<EncounterEntry>? Table { get; set; }
}

public sealed class EncounterEntry
{
    public string Id { get; set; } = "";
    public int Weight { get; set; } = 1;
}

public sealed class LightingSection
{
    public string? Ambient { get; set; }
    public List<string>? EmissiveTextures { get; set; }
}