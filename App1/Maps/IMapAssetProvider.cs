using Microsoft.Xna.Framework.Graphics;

namespace Untolia.Maps;

public interface IMapAssetProvider
{
    // Path is relative to the map folder root (e.g., "Forest01_bg.png" or "encounters.json").
    Texture2D LoadTexture(string relativePath);
    string ReadAllText(string relativePath);
    bool Exists(string relativePath);
}