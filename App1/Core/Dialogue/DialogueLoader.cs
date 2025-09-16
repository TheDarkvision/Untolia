// Untolia.Core/Dialogue/DialogueLoader.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Untolia.Core;
using Untolia.Core.RPG;
using Untolia.Core.UI;

namespace Untolia.Core.Dialogue;

public static class DialogueLoader
{
    // Expected file: Content/Data/Dialogues/{key}.json
    // Format:
    // [
    //   { "speaker": "System", "text": "Welcome" },
    //   { "speaker": "Guide", "text": "Hello!" }
    // ]
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static DialogueLine[] Load(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Array.Empty<DialogueLine>();

        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Content", "Data", "Dialogues", $"{key}.json");
        var devPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", "Data", "Dialogues", $"{key}.json"));

        string? path = File.Exists(runtimePath) ? runtimePath : (File.Exists(devPath) ? devPath : null);
        if (path == null)
        {
            Globals.Log.Warn($"DialogueLoader: dialogue file not found for key '{key}'");
            return Array.Empty<DialogueLine>();
        }

        try
        {
            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<DialogueEntry>>(json, Options) ?? new();
            var lines = new List<DialogueLine>(list.Count);
            foreach (var e in list)
            {
                var speaker = e.Speaker ?? "??";
                var text = e.Text ?? "";
                lines.Add(new DialogueLine(speaker, text));
            }
            return lines.ToArray();
        }
        catch (Exception ex)
        {
            Globals.Log.Error($"DialogueLoader: failed to parse '{path}': {ex.Message}");
            return Array.Empty<DialogueLine>();
        }
    }

    private sealed class DialogueEntry
    {
        public string? Speaker { get; set; }
        public string? Text { get; set; }
    }
}
