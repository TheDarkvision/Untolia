using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Untolia.Maps;

public static class MapLoader
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static MapData Load(GraphicsDevice gd, IMapAssetProvider provider, string manifestFileName)
    {
        // Read manifest (e.g., "map_data.json" in the map folder)
        var manifestJson = provider.ReadAllText(manifestFileName);
        var manifest = JsonSerializer.Deserialize<MapManifest>(manifestJson, JsonOpts)
                       ?? throw new InvalidDataException("Invalid map manifest JSON.");

        T ReadJson<T>(string path)
        {
            return JsonSerializer.Deserialize<T>(provider.ReadAllText(path), JsonOpts)
                   ?? throw new InvalidDataException($"Invalid JSON: {path}");
        }

        PortalsConfig ReadPortals(string path)
        {
            var raw = provider.ReadAllText(path).TrimStart();
            // Support both array root and object root with { "portals": [...] }
            if (raw.StartsWith("["))
            {
                var list = JsonSerializer.Deserialize<List<Portal>>(raw, JsonOpts)
                           ?? throw new InvalidDataException($"Invalid portals array: {path}");
                return new PortalsConfig { Portals = list };
            }

            return JsonSerializer.Deserialize<PortalsConfig>(raw, JsonOpts)
                   ?? throw new InvalidDataException($"Invalid portals object: {path}");
        }

        NpcsConfig ReadNpcs(string path)
        {
            var raw = provider.ReadAllText(path).TrimStart();
            // Support both array root and object root with { "npcs": [...] }
            if (raw.StartsWith("["))
            {
                var list = JsonSerializer.Deserialize<List<Npc>>(raw, JsonOpts)
                           ?? throw new InvalidDataException($"Invalid NPCs array: {path}");
                return new NpcsConfig { Npcs = list };
            }

            return JsonSerializer.Deserialize<NpcsConfig>(raw, JsonOpts)
                   ?? throw new InvalidDataException($"Invalid NPCs object: {path}");
        }

        var data = new MapData
        {
            Id = manifest.Id,
            Size = new Point(manifest.Size.Width, manifest.Size.Height),
            Bg = provider.LoadTexture(manifest.Assets.Bg),
            Over = provider.LoadTexture(manifest.Assets.Over),
            CollisionMask = provider.LoadTexture(manifest.Assets.Collision),
            EncountersMask = manifest.Assets.EncountersMask != null && provider.Exists(manifest.Assets.EncountersMask)
                ? provider.LoadTexture(manifest.Assets.EncountersMask)
                : null,
            LightingMask = manifest.Assets.Lighting != null && provider.Exists(manifest.Assets.Lighting)
                ? provider.LoadTexture(manifest.Assets.Lighting)
                : null,
            Encounters = ReadJson<EncountersConfig>(manifest.Data.Encounters),
            Puzzles = !string.IsNullOrWhiteSpace(manifest.Data.Puzzles) && provider.Exists(manifest.Data.Puzzles)
                ? ReadJson<PuzzlesConfig>(manifest.Data.Puzzles)
                : null,
            Portals = ReadPortals(manifest.Data.Portals),
            Npcs = ReadNpcs(manifest.Data.Npcs),
            Events = ReadJson<EventsConfig>(manifest.Data.Events),
            Settings = manifest.Data.Settings != null
                ? ReadJson<SettingsConfig>(manifest.Data.Settings)
                : manifest.Data.Meta != null
                    ? ReadJson<SettingsConfig>(manifest.Data.Meta)
                    : new SettingsConfig(),
            Spawn = manifest.Spawn,
            CameraBounds = new Rectangle(manifest.CameraBounds.X, manifest.CameraBounds.Y, manifest.CameraBounds.Width,
                manifest.CameraBounds.Height)
        };

        MapValidator.ValidateDimensions(data);

        // Build collision blocked map from collision mask
        var blocked = CollisionMaskHelper.BuildBlocked(data.CollisionMask, out var cw, out var ch);
        data = new MapData
        {
            Id = data.Id,
            Size = data.Size,
            Bg = data.Bg,
            Over = data.Over,
            CollisionMask = data.CollisionMask,
            EncountersMask = data.EncountersMask,
            LightingMask = data.LightingMask,
            Encounters = data.Encounters,
            Puzzles = data.Puzzles,
            Portals = data.Portals,
            Npcs = data.Npcs,
            Events = data.Events,
            Settings = data.Settings,
            Spawn = data.Spawn,
            CameraBounds = data.CameraBounds,
            CollisionBlocked = blocked,
            CollisionWidth = cw,
            CollisionHeight = ch
        };

        return data;
    }
}