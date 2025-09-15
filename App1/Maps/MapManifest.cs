using System.Text.Json.Serialization;

namespace Untolia.Maps;

public sealed class MapManifest
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";

    [JsonPropertyName("size")] public required MapSize Size { get; init; }

    [JsonPropertyName("assets")] public MapAssets Assets { get; init; } = new();

    [JsonPropertyName("data")] public MapDataFiles Data { get; init; } = new();

    [JsonPropertyName("spawn")] public SpawnInfo Spawn { get; init; } = new();

    [JsonPropertyName("cameraBounds")] public required Rect CameraBounds { get; init; }

    [JsonPropertyName("version")] public int Version { get; init; } = 1;

    public sealed class MapSize
    {
        [JsonPropertyName("width")] public int Width { get; init; }
        [JsonPropertyName("height")] public int Height { get; init; }
    }

    public sealed class MapAssets
    {
        [JsonPropertyName("bg")] public string Bg { get; init; } = "";
        [JsonPropertyName("collision")] public string Collision { get; init; } = "";
        [JsonPropertyName("over")] public string Over { get; init; } = "";
        [JsonPropertyName("encountersMask")] public string? EncountersMask { get; init; }
        [JsonPropertyName("lighting")] public string? Lighting { get; init; }
    }

    public sealed class MapDataFiles
    {
        [JsonPropertyName("encounters")] public string Encounters { get; init; } = "";
        [JsonPropertyName("puzzles")] public string? Puzzles { get; init; }
        [JsonPropertyName("portals")] public string Portals { get; init; } = "";
        [JsonPropertyName("npcs")] public string Npcs { get; init; } = "";
        [JsonPropertyName("events")] public string Events { get; init; } = "";
        [JsonPropertyName("meta")] public string? Meta { get; init; }
        [JsonPropertyName("settings")] public string? Settings { get; init; }
    }
}

public sealed class Rect
{
    [JsonPropertyName("x")] public int X { get; init; }
    [JsonPropertyName("y")] public int Y { get; init; }
    [JsonPropertyName("width")] public int Width { get; init; }
    [JsonPropertyName("height")] public int Height { get; init; }
}

public sealed class SpawnInfo
{
    [JsonPropertyName("x")] public int X { get; init; }
    [JsonPropertyName("y")] public int Y { get; init; }
    [JsonPropertyName("facing")] public string Facing { get; init; } = "south";
}