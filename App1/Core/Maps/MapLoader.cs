using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Untolia.Core.Maps;

public static class MapLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    // baseDirRelativeToContent example: "Maps/Forest01" (case-sensitive on Linux/macOS)
    public static MapData LoadFromManifest(GraphicsDevice gd, ContentManager content, string baseDirRelativeToContent)
    {
        string ResolveBaseDir(string rel)
        {
            // Runtime Content folder
            var abs = Path.Combine(AppContext.BaseDirectory, "Content", rel);
            if (Directory.Exists(abs)) return abs;

            // Try toggling first segment casing: "Maps" <-> "maps"
            var parts = rel.Split(new[] { '/', '\\' }, 2);
            if (parts.Length > 0)
            {
                var altFirst = parts[0] == "Maps" ? "maps" : parts[0] == "maps" ? "Maps" : parts[0];
                var altRel = parts.Length == 2 ? Path.Combine(altFirst, parts[1]) : altFirst;
                var altAbs = Path.Combine(AppContext.BaseDirectory, "Content", altRel);
                if (Directory.Exists(altAbs)) return altAbs;
            }

            // Dev-time fallback (when running from bin/, read project Content/)
            var dev = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", rel));
            if (Directory.Exists(dev)) return dev;

            return abs;
        }

        var baseDirAbs = ResolveBaseDir(baseDirRelativeToContent);
        var manifestPath = Path.Combine(baseDirAbs, "map.json");

        if (File.Exists(manifestPath))
        {
            Globals.Log.Info($"MapLoader: Reading manifest: {manifestPath}");
            var json = File.ReadAllText(manifestPath);
            var m = JsonSerializer.Deserialize<MapManifest>(json, Options)
                    ?? throw new InvalidDataException("Invalid map manifest json");

            var portals = ParsePortals(json);
            var eventsList = ParseEvents(json);

            Globals.Log.Debug(
                $"MapLoader: id='{m.Id}', bg='{m.Layers?.Background}', over='{m.Layers?.Overhead}', size=({m.Size?.Width}x{m.Size?.Height})");

            // Background is required; fallback to solid black if missing
            var bgTex = TryLoadTexture(content, gd, baseDirRelativeToContent, m.Layers?.Background);
            if (bgTex == null)
            {
                Globals.Log.Warn(
                    $"MapLoader: Background not found, using solid black. rel='{baseDirRelativeToContent}', file='{m.Layers?.Background}'");
                bgTex = MakeSolidTexture(gd, Color.Black);
            }

            // Overhead is truly optional: set to null if missing so it won't render
            Texture2D? overTex = null;
            if (!string.IsNullOrWhiteSpace(m.Layers?.Overhead))
            {
                overTex = TryLoadTexture(content, gd, baseDirRelativeToContent, m.Layers.Overhead);
                if (overTex == null)
                    Globals.Log.Debug(
                        $"MapLoader: Overhead not found (optional). rel='{baseDirRelativeToContent}', file='{m.Layers.Overhead}'");
                else
                    Globals.Log.Info("MapLoader: Loaded overhead OK.");
            }

            var widthPx = m.Size?.Width ?? (bgTex.Width > 0 ? bgTex.Width : Globals.ScreenSize.X);
            var heightPx = m.Size?.Height ?? (bgTex.Height > 0 ? bgTex.Height : Globals.ScreenSize.Y);

            var rawMask = Array.Empty<bool>();
            int maskW = 0, maskH = 0;

            if (!string.IsNullOrWhiteSpace(m.Masks?.Collision))
            {
                var colFull = Path.Combine(baseDirAbs, m.Masks.Collision);
                if (File.Exists(colFull))
                {
                    Globals.Log.Info($"MapLoader: Loading collision mask: {colFull}");
                    using var fs = File.OpenRead(colFull);
                    using var colTex = Texture2D.FromStream(gd, fs);
                    maskW = colTex.Width;
                    maskH = colTex.Height;
                    rawMask = SampleCollisionMaskArray(colTex);
                }
                else
                {
                    Globals.Log.Warn($"MapLoader: Collision mask file not found: {colFull}");
                }
            }

            var fittedMask = FitMaskToSize(rawMask, maskW, maskH, widthPx, heightPx);

            var id = string.IsNullOrWhiteSpace(m.Id) ? Path.GetFileName(baseDirAbs) : m.Id;
            Globals.Log.Info(
                $"MapLoader: Built map '{id}' size=({widthPx}x{heightPx}) collisionLen={fittedMask.Length}");

            // Post-validate portals against map bounds to help debugging
            if (portals.Count > 0)
            {
                Globals.Log.Info($"MapLoader: {portals.Count} portal(s) parsed for '{id}':");
                foreach (var p in portals)
                {
                    var r = p.Area;
                    var outOfBounds = r.Left < 0 || r.Top < 0 || r.Right > widthPx || r.Bottom > heightPx;
                    var msg =
                        $"  portal id='{p.Id}' rect=({r.X},{r.Y},{r.Width},{r.Height}) target='{p.TargetMap}' spawn=({p.TargetSpawn.X},{p.TargetSpawn.Y})";
                    if (outOfBounds)
                        Globals.Log.Warn("MapLoader: " + msg + " [OUT OF BOUNDS]");
                    else
                        Globals.Log.Debug("MapLoader: " + msg);
                }
            }

            return new MapData
            {
                Id = id,
                Size = new Point(widthPx, heightPx),
                CollisionWidth = widthPx,
                CollisionHeight = heightPx,
                Spawn = new Point(m.PlayerSpawn?.X ?? 0, m.PlayerSpawn?.Y ?? 0),
                Bg = bgTex,
                Over = overTex, // may be null -> will not render
                CollisionBlocked = fittedMask,
                CameraBounds = new Rectangle(0, 0, widthPx, heightPx),
                Portals = portals,
                Events = eventsList
            };
        }


        Globals.Log.Warn($"MapLoader: Manifest not found, using heuristic load from '{baseDirAbs}'");
        return BuildFromFolderHeuristics(gd, baseDirAbs);
    }

    private static MapData BuildFromFolderHeuristics(GraphicsDevice gd, string baseDirAbs)
    {
        // If not present under runtime Content, try dev Content
        if (!Directory.Exists(baseDirAbs))
        {
            var relFromContent = Path.GetRelativePath(Path.Combine(AppContext.BaseDirectory, "Content"), baseDirAbs);
            var devBase =
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", relFromContent));
            if (Directory.Exists(devBase))
                baseDirAbs = devBase;
        }

        var bgPath = Path.Combine(baseDirAbs, "layer_background.png");
        var overPath = Path.Combine(baseDirAbs, "layer_overhead.png");
        var colPath = Path.Combine(baseDirAbs, "mask_collision.png");

        var bgTex = File.Exists(bgPath) ? LoadTextureRaw(gd, bgPath) : MakeSolidTexture(gd, Color.Black);

        // Overhead optional via heuristic as well
        var overTex = File.Exists(overPath) ? LoadTextureRaw(gd, overPath) : null;

        var widthPx = bgTex.Width > 0 ? bgTex.Width : Globals.ScreenSize.X;
        var heightPx = bgTex.Height > 0 ? bgTex.Height : Globals.ScreenSize.Y;

        var rawMask = Array.Empty<bool>();
        int maskW = 0, maskH = 0;

        if (File.Exists(colPath))
        {
            Globals.Log.Info($"MapLoader: Heuristic collision mask: {colPath}");
            using var fs = File.OpenRead(colPath);
            using var tex = Texture2D.FromStream(gd, fs);
            maskW = tex.Width;
            maskH = tex.Height;
            rawMask = SampleCollisionMaskArray(tex);
        }

        var fittedMask = FitMaskToSize(rawMask, maskW, maskH, widthPx, heightPx);

        var id = Path.GetFileName(baseDirAbs);
        Globals.Log.Info(
            $"MapLoader: Heuristic map '{id}' size=({widthPx}x{heightPx}) collisionLen={fittedMask.Length}");

        return new MapData
        {
            Id = id,
            Size = new Point(widthPx, heightPx),
            CollisionWidth = widthPx,
            CollisionHeight = heightPx,
            Spawn = new Point(0, 0),
            Bg = bgTex,
            Over = overTex, // may be null
            CollisionBlocked = fittedMask,
            CameraBounds = new Rectangle(0, 0, widthPx, heightPx),
            Portals = Array.Empty<Portal>(),
            Events = Array.Empty<MapEvent>()
        };
    }


    // Try ContentManager (XNB) first, then raw file (runtime/dev)
    private static Texture2D? TryLoadTexture(ContentManager content, GraphicsDevice gd, string baseDirRel,
        string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;

        // 1) ContentManager (XNB)
        try
        {
            var assetNoExt = Path.ChangeExtension(Path.Combine(baseDirRel, fileName).Replace('\\', '/'), null);
            if (!string.IsNullOrWhiteSpace(assetNoExt))
            {
                Globals.Log.Debug($"MapLoader: Content.Load '{assetNoExt}'");
                return content.Load<Texture2D>(assetNoExt);
            }
        }
        catch
        {
            // ignore and try raw file
        }

        // 2) Raw file runtime Content
        var runtimeFull = Path.Combine(AppContext.BaseDirectory, "Content", baseDirRel, fileName);
        if (File.Exists(runtimeFull))
        {
            Globals.Log.Debug($"MapLoader: Raw file load (runtime) '{runtimeFull}'");
            using var fs = File.OpenRead(runtimeFull);
            return Texture2D.FromStream(gd, fs);
        }

        // 3) Raw file dev Content
        var devFull = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", baseDirRel,
            fileName));
        if (File.Exists(devFull))
        {
            Globals.Log.Debug($"MapLoader: Raw file load (dev) '{devFull}'");
            using var fs = File.OpenRead(devFull);
            return Texture2D.FromStream(gd, fs);
        }

        Globals.Log.Warn($"MapLoader: Texture not found: rel='{baseDirRel}', file='{fileName}'");
        return null;
    }

    private static Texture2D LoadTextureRaw(GraphicsDevice gd, string fullPath)
    {
        using var fs = File.OpenRead(fullPath);
        return Texture2D.FromStream(gd, fs);
    }

    private static Texture2D MakeSolidTexture(GraphicsDevice gd, Color color)
    {
        var tex = new Texture2D(gd, 1, 1);
        tex.SetData(new[] { color });
        return tex;
    }

    // Flat array [y * width + x], black & opaque => blocked
    private static bool[] SampleCollisionMaskArray(Texture2D tex)
    {
        int w = tex.Width, h = tex.Height;
        var pixels = new Color[w * h];
        tex.GetData(pixels);

        var blocked = new bool[w * h];
        for (var y = 0; y < h; y++)
        {
            var row = y * w;
            for (var x = 0; x < w; x++)
            {
                var c = pixels[row + x];
                blocked[row + x] = c.A > 128 && c.R < 16 && c.G < 16 && c.B < 16;
            }
        }

        return blocked;
    }

    // Ensure resulting array length = targetW * targetH. Copy overlap only; default false elsewhere.
    private static bool[] FitMaskToSize(bool[] src, int srcW, int srcH, int targetW, int targetH)
    {
        var dst = new bool[targetW * targetH];

        if (src.Length == 0 || srcW <= 0 || srcH <= 0) return dst;

        var copyW = Math.Min(srcW, targetW);
        var copyH = Math.Min(srcH, targetH);

        for (var y = 0; y < copyH; y++)
        {
            var srcRow = y * srcW;
            var dstRow = y * targetW;
            for (var x = 0; x < copyW; x++) dst[dstRow + x] = src[srcRow + x];
        }

        return dst;
    }

    private static List<Portal> ParsePortals(string json)
    {
        var list = new List<Portal>();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("portals", out var portalsEl) || portalsEl.ValueKind != JsonValueKind.Array)
            return list;

        // Allow tile-based portal definitions by reading tileSize from the manifest (fallback to global)
        var tileSize = Globals.TileSize;
        if (root.TryGetProperty("tileSize", out var tsEl) && tsEl.ValueKind == JsonValueKind.Number)
            try
            {
                tileSize = tsEl.GetInt32();
            }
            catch
            {
                /* ignore */
            }

        foreach (var p in portalsEl.EnumerateArray())
        {
            var id = p.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String
                ? idEl.GetString() ?? ""
                : "";

            // Prefer pixel-based rectangle if fully provided; otherwise fall back to tile-based fields
            var x = p.TryGetProperty("x", out var xEl) && xEl.ValueKind == JsonValueKind.Number
                ? xEl.GetInt32()
                : int.MinValue;
            var y = p.TryGetProperty("y", out var yEl) && yEl.ValueKind == JsonValueKind.Number
                ? yEl.GetInt32()
                : int.MinValue;
            var w = p.TryGetProperty("width", out var wEl) && wEl.ValueKind == JsonValueKind.Number
                ? wEl.GetInt32()
                : int.MinValue;
            var h = p.TryGetProperty("height", out var hEl) && hEl.ValueKind == JsonValueKind.Number
                ? hEl.GetInt32()
                : int.MinValue;

            // Tile-based fallback
            if (x == int.MinValue || y == int.MinValue || w == int.MinValue || h == int.MinValue)
            {
                var tx = p.TryGetProperty("tileX", out var txEl) && txEl.ValueKind == JsonValueKind.Number
                    ? txEl.GetInt32()
                    : int.MinValue;
                var ty = p.TryGetProperty("tileY", out var tyEl) && tyEl.ValueKind == JsonValueKind.Number
                    ? tyEl.GetInt32()
                    : int.MinValue;
                var tw = p.TryGetProperty("tileW", out var twEl) && twEl.ValueKind == JsonValueKind.Number
                    ? twEl.GetInt32()
                    : 1;
                var th = p.TryGetProperty("tileH", out var thEl) && thEl.ValueKind == JsonValueKind.Number
                    ? thEl.GetInt32()
                    : 1;

                if (tx != int.MinValue && ty != int.MinValue && tw > 0 && th > 0)
                {
                    x = tx * tileSize;
                    y = ty * tileSize;
                    w = tw * tileSize;
                    h = th * tileSize;
                }
            }

            var targetMap = p.TryGetProperty("targetMap", out var tmEl) && tmEl.ValueKind == JsonValueKind.String
                ? tmEl.GetString() ?? ""
                : "";

            int spawnX = 0, spawnY = 0;
            if (p.TryGetProperty("targetSpawn", out var tsObj) && tsObj.ValueKind == JsonValueKind.Object)
            {
                // Support either pixel spawn or tile-based spawn inside targetSpawn
                if (tsObj.TryGetProperty("x", out var sxEl) && sxEl.ValueKind == JsonValueKind.Number)
                    spawnX = sxEl.GetInt32();
                if (tsObj.TryGetProperty("y", out var syEl) && syEl.ValueKind == JsonValueKind.Number)
                    spawnY = syEl.GetInt32();

                // Optional tile-based target spawn
                if ((spawnX == 0 && spawnY == 0) || // if not explicitly set above
                    tsObj.TryGetProperty("tileX", out var tsxEl) || tsObj.TryGetProperty("tileY", out var tsyEl))
                {
                    var tsx = tsObj.TryGetProperty("tileX", out var tsxEl2) && tsxEl2.ValueKind == JsonValueKind.Number
                        ? tsxEl2.GetInt32()
                        : spawnX / tileSize;
                    var tsy = tsObj.TryGetProperty("tileY", out var tsyEl2) && tsyEl2.ValueKind == JsonValueKind.Number
                        ? tsyEl2.GetInt32()
                        : spawnY / tileSize;
                    spawnX = tsx * tileSize;
                    spawnY = tsy * tileSize;
                }
            }

            if (w > 0 && h > 0 && !string.IsNullOrWhiteSpace(targetMap) && x != int.MinValue && y != int.MinValue)
                list.Add(new Portal
                {
                    Id = id,
                    Area = new Rectangle(x, y, w, h),
                    TargetMap = targetMap,
                    TargetSpawn = new Point(spawnX, spawnY)
                });
        }

        return list;
    }


    private static List<MapEvent> ParseEvents(string json)
    {
        var list = new List<MapEvent>();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("events", out var eventsEl) || eventsEl.ValueKind != JsonValueKind.Array)
            return list;

        // Read optional tileSize from manifest (fallback to global)
        var tileSize = Globals.TileSize;
        if (root.TryGetProperty("tileSize", out var tsEl) && tsEl.ValueKind == JsonValueKind.Number)
            try
            {
                tileSize = tsEl.GetInt32();
            }
            catch
            {
                /* ignore */
            }

        foreach (var e in eventsEl.EnumerateArray())
        {
            var id = e.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String
                ? idEl.GetString() ?? ""
                : "";
            var type = e.TryGetProperty("type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String
                ? typeEl.GetString() ?? ""
                : "";
            var when = e.TryGetProperty("when", out var whenEl) && whenEl.ValueKind == JsonValueKind.String
                ? whenEl.GetString() ?? ""
                : "";

            var flag = e.TryGetProperty("flag", out var flagEl) && flagEl.ValueKind == JsonValueKind.String
                ? flagEl.GetString()
                : null;
            var value = e.TryGetProperty("value", out var valEl) &&
                        (valEl.ValueKind == JsonValueKind.True || valEl.ValueKind == JsonValueKind.False)
                ? valEl.GetBoolean()
                : (bool?)null;

            var dialogueKey = e.TryGetProperty("dialogueKey", out var dkEl) && dkEl.ValueKind == JsonValueKind.String
                ? dkEl.GetString()
                : null;

            // Support either "oneTime": true or shorthand "once": true
            var oneTime =
                (e.TryGetProperty("oneTime", out var otEl) &&
                 (otEl.ValueKind == JsonValueKind.True || otEl.ValueKind == JsonValueKind.False) && otEl.GetBoolean())
                || (e.TryGetProperty("once", out var onceEl) &&
                    (onceEl.ValueKind == JsonValueKind.True || onceEl.ValueKind == JsonValueKind.False) &&
                    onceEl.GetBoolean());

            // Optional area from pixel coords
            Rectangle? area = null;
            var x = e.TryGetProperty("x", out var xEl) && xEl.ValueKind == JsonValueKind.Number
                ? xEl.GetInt32()
                : int.MinValue;
            var y = e.TryGetProperty("y", out var yEl) && yEl.ValueKind == JsonValueKind.Number
                ? yEl.GetInt32()
                : int.MinValue;
            var w = e.TryGetProperty("width", out var wEl) && wEl.ValueKind == JsonValueKind.Number
                ? wEl.GetInt32()
                : int.MinValue;
            var h = e.TryGetProperty("height", out var hEl) && hEl.ValueKind == JsonValueKind.Number
                ? hEl.GetInt32()
                : int.MinValue;

            if (x != int.MinValue && y != int.MinValue && w != int.MinValue && h != int.MinValue && w > 0 && h > 0)
            {
                area = new Rectangle(x, y, w, h);
            }
            else
            {
                // Optional area from tile coords
                var tx = e.TryGetProperty("tileX", out var txEl) && txEl.ValueKind == JsonValueKind.Number
                    ? txEl.GetInt32()
                    : int.MinValue;
                var ty = e.TryGetProperty("tileY", out var tyEl) && tyEl.ValueKind == JsonValueKind.Number
                    ? tyEl.GetInt32()
                    : int.MinValue;
                var tw = e.TryGetProperty("tileW", out var twEl) && twEl.ValueKind == JsonValueKind.Number
                    ? twEl.GetInt32()
                    : 1;
                var th = e.TryGetProperty("tileH", out var thEl) && thEl.ValueKind == JsonValueKind.Number
                    ? thEl.GetInt32()
                    : 1;

                if (tx != int.MinValue && ty != int.MinValue && tw > 0 && th > 0)
                    area = new Rectangle(tx * tileSize, ty * tileSize, tw * tileSize, th * tileSize);
            }

            if (!string.IsNullOrWhiteSpace(type))
                list.Add(new MapEvent
                {
                    Id = id,
                    Type = type,
                    When = when,
                    Flag = flag,
                    Value = value,
                    DialogueKey = dialogueKey,
                    OneTime = oneTime,
                    Area = area
                });
        }

        return list;
    }
}