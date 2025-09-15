using System.Text.Json;
using System.Text.Json.Serialization;

namespace Untolia.Maps;

// Encounters.json
public sealed class EncountersConfig
{
    [JsonPropertyName("zoneColors")] public Dictionary<string, string> ZoneColors { get; init; } = new();

    [JsonPropertyName("defaults")] public EncounterDefaults Defaults { get; init; } = new();

    [JsonPropertyName("defaultEncounterRate")]
    public int? DefaultEncounterRate { get; init; }
}

public sealed class EncounterDefaults
{
    [JsonPropertyName("stepsPerCheck")] public int StepsPerCheck { get; init; } = 22;
    [JsonPropertyName("chancePercent")] public int ChancePercent { get; init; } = 12;
}

// Puzzles
public sealed class PuzzlesConfig
{
    [JsonPropertyName("gridSize")] public int GridSize { get; init; } = 32;

    [JsonPropertyName("blocks")] public List<PuzzleBlock> Blocks { get; init; } = new();
    [JsonPropertyName("targets")] public List<GridPoint> Targets { get; init; } = new();
    [JsonPropertyName("switches")] public List<PuzzleSwitch> Switches { get; init; } = new();
}

public sealed class PuzzleBlock
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("x")] public int X { get; init; }
    [JsonPropertyName("y")] public int Y { get; init; }
}

public sealed class GridPoint
{
    [JsonPropertyName("x")] public int X { get; init; }
    [JsonPropertyName("y")] public int Y { get; init; }
}

public sealed class PuzzleSwitch
{
    [JsonPropertyName("x")] public int X { get; init; }
    [JsonPropertyName("y")] public int Y { get; init; }
    [JsonPropertyName("event")] public string Event { get; init; } = "";
}

// Portals (updated for array root with nested area/dest/conditions)
public sealed class PortalsConfig
{
    [JsonPropertyName("portals")] public List<Portal> Portals { get; init; } = new();
}

public sealed class Portal
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";

    [JsonPropertyName("area")] public PortalArea Area { get; init; } = new();

    [JsonPropertyName("dest")] public PortalDestination Dest { get; init; } = new();

    [JsonPropertyName("conditions")] public PortalConditions? Conditions { get; init; }
}

public sealed class PortalArea
{
    [JsonPropertyName("x")] public int X { get; init; }
    [JsonPropertyName("y")] public int Y { get; init; }
    [JsonPropertyName("w")] public int W { get; init; }
    [JsonPropertyName("h")] public int H { get; init; }
}

public sealed class PortalDestination
{
    [JsonPropertyName("map")] public string Map { get; init; } = "";
    [JsonPropertyName("x")] public int X { get; init; }
    [JsonPropertyName("y")] public int Y { get; init; }
    [JsonPropertyName("facing")] public string? Facing { get; init; }
}

public sealed class PortalConditions
{
    [JsonPropertyName("requireEvent")] public string? RequireEvent { get; init; }
}

// NPCs
public sealed class NpcsConfig
{
    [JsonPropertyName("npcs")] public List<Npc> Npcs { get; init; } = new();
}

public sealed class Npc
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("x")] public int X { get; init; }
    [JsonPropertyName("y")] public int Y { get; init; }
    [JsonPropertyName("sprite")] public string Sprite { get; init; } = "";
    [JsonPropertyName("dialogue")] public string? Dialogue { get; init; }
    [JsonPropertyName("shopInventory")] public string? ShopInventory { get; init; }
    [JsonPropertyName("facing")] public string Facing { get; init; } = "south";
    [JsonPropertyName("path")] public List<PathPoint>? Path { get; init; }
    [JsonPropertyName("scheduleFlag")] public string? ScheduleFlag { get; init; }

    [JsonPropertyName("interactionRadius")]
    public int InteractionRadius { get; init; } = 16;
}

public sealed class PathPoint
{
    [JsonPropertyName("x")] public int X { get; init; }
    [JsonPropertyName("y")] public int Y { get; init; }
    [JsonPropertyName("waitMs")] public int WaitMs { get; init; } = 0;
}

// Events (updated for triggers/cutscenes/autoStart)
public sealed class EventsConfig
{
    [JsonPropertyName("triggers")] public List<EventTrigger> Triggers { get; init; } = new();

    [JsonPropertyName("cutscenes")] public Dictionary<string, List<EventAction>> Cutscenes { get; init; } = new();

    [JsonPropertyName("autoStart")] public List<AutoStartEvent> AutoStart { get; init; } = new();
}

public sealed class EventTrigger
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";

    [JsonPropertyName("area")] public EventArea Area { get; init; } = new();

    [JsonPropertyName("once")] public bool Once { get; init; } = false;

    [JsonPropertyName("onEnter")] public List<EventAction> OnEnter { get; init; } = new();
}

public sealed class EventArea
{
    [JsonPropertyName("x")] public int X { get; init; }
    [JsonPropertyName("y")] public int Y { get; init; }
    [JsonPropertyName("w")] public int W { get; init; }
    [JsonPropertyName("h")] public int H { get; init; }
}

public sealed class EventAction
{
    [JsonPropertyName("type")] public string Type { get; init; } = "";

    // Collect any extra fields like id, qty, text, x, y, durationMs, etc.
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; init; }
}

public sealed class AutoStartEvent
{
    [JsonPropertyName("when")] public string When { get; init; } = "";
    [JsonPropertyName("run")] public string Run { get; init; } = "";
}

// Settings
public sealed class SettingsConfig
{
    [JsonPropertyName("music")] public string? Music { get; init; }
    [JsonPropertyName("encounterRate")] public int? EncounterRate { get; init; }
    [JsonPropertyName("weather")] public string? Weather { get; init; }
    [JsonPropertyName("ambientSound")] public string? AmbientSound { get; init; }
    [JsonPropertyName("battleBackground")] public string? BattleBackground { get; init; }
    [JsonPropertyName("footstepMap")] public string? FootstepMap { get; init; }
}