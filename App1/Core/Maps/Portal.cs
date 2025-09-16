// Untolia.Core/Maps/Portal.cs

using Microsoft.Xna.Framework;

namespace Untolia.Core.Maps;

public sealed class Portal
{
    public string Id { get; init; } = "";
    public Rectangle Area { get; init; }
    public string TargetMap { get; init; } = "";
    public Point TargetSpawn { get; init; }
}