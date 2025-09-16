using System.Text.Json;

namespace Untolia.Core.RPG;

public sealed class CharacterRegistry
{
    private readonly Dictionary<string, CharacterDef> _defs = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, CharacterDef> All => _defs;

    // contentRootRelativeDir: e.g., "data/characters"
    public void LoadAllFromFolder(string contentRootRelativeDir)
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Content", contentRootRelativeDir);
        if (!Directory.Exists(folder))
        {
            Globals.Log.Warn($"Characters: Folder not found: {folder}");
            return;
        }

        var jsonFiles = Directory.EnumerateFiles(folder, "*.json", SearchOption.TopDirectoryOnly).ToList();
        Globals.Log.Info($"Characters: Loading {jsonFiles.Count} file(s) from {folder}");

        foreach (var file in jsonFiles)
            try
            {
                var json = File.ReadAllText(file);
                var def = JsonSerializer.Deserialize<CharacterDef>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });
                if (def == null || string.IsNullOrWhiteSpace(def.Id))
                {
                    Globals.Log.Warn($"Characters: Skipped invalid definition: {file}");
                    continue;
                }

                _defs[def.Id] = def;
                Globals.Log.Debug($"Characters: Loaded '{def.Id}' ('{def.Name}')");
            }
            catch (Exception ex)
            {
                Globals.Log.Error(ex, $"Characters: Failed to parse {file}");
            }
    }

    public CharacterDef? Get(string id)
    {
        return _defs.TryGetValue(id, out var d) ? d : null;
    }
}