using Microsoft.Xna.Framework;

namespace Untolia.Core.Maps;

public sealed class MapEvent
{
    public string Id { get; init; } = "";
    public string Type { get; init; } = ""; // e.g., "setFlag", "showDialogue"
    public string When { get; init; } = ""; // e.g., "onEnter", "onTileEnter"

    // setFlag payload
    public string? Flag { get; init; }
    public bool? Value { get; init; }

    // showDialogue payload
    public string? DialogueKey { get; init; }

    // If true, the event runs only once per game session
    public bool OneTime { get; init; } = false;

    // Optional area in pixels for tile-based triggers (computed during load)
    public Rectangle? Area { get; init; }
}