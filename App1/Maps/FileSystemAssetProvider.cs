using Microsoft.Xna.Framework.Graphics;

namespace Untolia.Maps;

// Use when you keep raw PNGs/JSON and copy the entire folder to output (e.g., "Assets/maps/Forest01")
public sealed class FileSystemAssetProvider : IMapAssetProvider
{
    private readonly GraphicsDevice _gd;
    private readonly string _mapRoot; // absolute or relative base path to the map folder

    public FileSystemAssetProvider(GraphicsDevice gd, string mapRoot)
    {
        _gd = gd;
        _mapRoot = mapRoot;
    }

    public Texture2D LoadTexture(string relativePath)
    {
        using var fs = File.OpenRead(Path.Combine(_mapRoot, relativePath));
        return Texture2D.FromStream(_gd, fs);
    }

    public string ReadAllText(string relativePath)
    {
        return File.ReadAllText(Path.Combine(_mapRoot, relativePath));
    }

    public bool Exists(string relativePath)
    {
        return File.Exists(Path.Combine(_mapRoot, relativePath));
    }
}