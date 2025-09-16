using System.Text.Json;

namespace Untolia.Core.Inventory;

public static class ItemLoader
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private static readonly Dictionary<string, ItemCategory> FolderToCategory = new(StringComparer.OrdinalIgnoreCase)
    {
        { "consumables", ItemCategory.Items },
        { "equipment", ItemCategory.Equipment },
        { "key", ItemCategory.KeyItems },
        { "keyitems", ItemCategory.KeyItems }
    };

    // Returns number of item defs loaded
    public static int LoadAllFromContent(InventoryService inventory, string itemsRoot = "Data/Items")
    {
        var candidateRoots = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Content", itemsRoot),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", itemsRoot))
        };

        string? root = null;
        foreach (var r in candidateRoots)
            if (Directory.Exists(r))
            {
                root = r;
                break;
            }

        if (root == null)
        {
            Globals.Log.Warn($"Inventory: Root not found for '{itemsRoot}'");
            return 0;
        }

        var loaded = 0;
        Globals.Log.Info($"Inventory: Scanning {root}");

        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            var folderName = Path.GetFileName(dir);
            Globals.Log.Debug($"Inventory: Folder {folderName}");

            if (!FolderToCategory.TryGetValue(folderName, out var category))
            {
                Globals.Log.Warn($"Inventory: Unknown folder '{folderName}', skipping");
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(dir, "*.json", SearchOption.TopDirectoryOnly))
                try
                {
                    var json = File.ReadAllText(file);
                    var def = JsonSerializer.Deserialize<InventoryItemDef>(json, JsonOpts);
                    if (def == null || string.IsNullOrWhiteSpace(def.Id))
                    {
                        Globals.Log.Warn($"Inventory: Invalid item def in {file}");
                        continue;
                    }

                    def.Category = category;
                    if (string.IsNullOrWhiteSpace(def.Name)) def.Name = def.Id;
                    if (def.StackLimit <= 0) def.StackLimit = category == ItemCategory.Items ? 99 : 1;

                    inventory.RegisterDef(def);
                    loaded++;
                    Globals.Log.Debug($"Inventory: Registered '{def.Id}' ({def.Category})");
                }
                catch (Exception ex)
                {
                    Globals.Log.Error(ex, $"Inventory: Failed to read {file}");
                }
        }

        Globals.Log.Info($"Inventory: Loaded {loaded} item definition(s)");
        return loaded;
    }
}