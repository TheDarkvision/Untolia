using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Untolia.Maps;

// Use when PNGs are built with Content Pipeline (no extensions in Load path).
// JSON files should be copied to output under the same relative folder.
public sealed class ContentPipelineAssetProvider : IMapAssetProvider
{
    private readonly ContentManager _content;
    private readonly GraphicsDevice _gd;
    private readonly string _mapContentPrefix; // e.g., "maps/Forest01/"
    private readonly string _mapOutputFolder; // e.g., "Content/maps/Forest01/"

    public ContentPipelineAssetProvider(ContentManager content, GraphicsDevice gd, string mapContentPrefix,
        string mapOutputFolder)
    {
        _content = content;
        _gd = gd;
        _mapContentPrefix = mapContentPrefix.TrimEnd('/') + "/";
        _mapOutputFolder = mapOutputFolder.TrimEnd('/') + "/";
    }

    public Texture2D LoadTexture(string relativePath)
    {
        // Remove extension for Content.Load
        var noExt = Path.ChangeExtension(relativePath, null) ?? relativePath;
        return _content.Load<Texture2D>(_mapContentPrefix + noExt);
    }

    public string ReadAllText(string relativePath)
    {
        var full = Path.Combine(_mapOutputFolder, relativePath);
        return File.ReadAllText(full);
    }

    public bool Exists(string relativePath)
    {
        var full = Path.Combine(_mapOutputFolder, relativePath);
        return File.Exists(full);
    }
}