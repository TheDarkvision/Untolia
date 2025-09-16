namespace Untolia.Core.RPG;

public sealed class CharacterDef
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";

    // New: portrait path relative to Content root
    public string? Portrait { get; set; }

    public PrimaryStats BaseStats { get; set; } = new();
    public GrowthDef Growth { get; set; } = new();
    public ExpCurveDef ExpCurve { get; set; } = new();
    public List<LearnDef> Learnset { get; set; } = new();

    // New: extra sections from JSON
    public ElementsDef? Elements { get; set; }
    public EquipmentDef? Equipment { get; set; }
    public AppearanceDef? Appearance { get; set; }
    public LoreDef? Lore { get; set; }
    public RoleDef? Role { get; set; }

    public List<string> Tags { get; set; } = new();
    public int StartingLevel { get; set; } = 1;

    public int Version { get; set; } = 1;
}

public sealed class GrowthDef
{
    // "keyframes" | "table" | "formula" (for now we implement keyframes)
    public string Model { get; set; } = "keyframes";
    public List<CurvePoint> Force { get; set; } = new();
    public List<CurvePoint> Focus { get; set; } = new();
    public List<CurvePoint> Resolve { get; set; } = new();
    public List<CurvePoint> Agility { get; set; } = new();
    public List<CurvePoint> Hp { get; set; } = new();
    public List<CurvePoint> Mp { get; set; } = new();
}

public sealed class CurvePoint
{
    public int Lvl { get; set; }
    public int Val { get; set; }
}

public sealed class ExpCurveDef
{
    public string Model { get; set; } = "linear";
    public int Base { get; set; } = 100;
    public int PerLevel { get; set; } = 50;

    public int ExpToNext(int level)
    {
        return Model switch
        {
            "linear" => Base + (level - 1) * PerLevel,
            _ => Base + (level - 1) * PerLevel
        };
    }
}

public sealed class LearnDef
{
    public int Level { get; set; }
    public string Ability { get; set; } = "";
}

// New: extra sections

public sealed class ElementsDef
{
    public List<string> Resist { get; set; } = new();
    public List<string> Weak { get; set; } = new();
    public List<string> Immune { get; set; } = new();
}

public sealed class EquipmentDef
{
    public string? Weapon { get; set; }
    public string? Shield { get; set; }
    public string? Armor { get; set; }
    public string? Trinket { get; set; }
}

public sealed class AppearanceDef
{
    public string? Age { get; set; }
    public string? Build { get; set; }
    public string? Hair { get; set; }
    public string? Eyes { get; set; }
    public string? Skin { get; set; }
    public string? Attire { get; set; }
}

public sealed class LoreDef
{
    public string? Origin { get; set; }
    public string? Motivation { get; set; }
    public string? Symbol { get; set; }
}

public sealed class RoleDef
{
    public string? BattleStyle { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
}