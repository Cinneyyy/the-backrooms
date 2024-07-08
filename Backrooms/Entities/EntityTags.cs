namespace Backrooms;

public record struct EntityTags
{
    public record struct Vector2(float x, float y)
    {
        public static implicit operator Vec2f(Vector2 v) => new(v.x, v.y);
    }

    public readonly record struct ManagedPathfinding(float speed, bool builtinAlgorithm, string algorithmName);


    public string instance { get; init; }
    public ManagedPathfinding? managedPathfinding { get; init; }
    public Vector2 size { get; set; }
    public string sprite, audio;
}