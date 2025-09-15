namespace Untolia.Core;

public class MapDefinition
{
    public string Name { get; set; } = "";
    public int TileSize { get; set; } = 16;
    public int Width { get; set; }
    public int Height { get; set; }
    public int[] Tiles { get; set; } = [];
    public Vec2 Spawn { get; set; } = new();
    public List<PortalDef> Portals { get; set; } = new();
}

public class PortalDef
{
    public RectI Rect { get; set; } = new();
    public string TargetMap { get; set; } = "";
    public Vec2 TargetSpawn { get; set; } = new();
}

public class Vec2
{
    public float X { get; set; }
    public float Y { get; set; }
}

public class RectI
{
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; }
    public int H { get; set; }
}