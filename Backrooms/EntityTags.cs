using System.Linq;

namespace Backrooms;

public record struct EntityTags
{
    public record struct Vector2(float x, float y)
    {
        public static implicit operator Vec2f(Vector2 v) => new(v.x, v.y);
    }
    public readonly record struct Function(string id, string name);
    public readonly record struct ManagedPathfinding(float speed, bool builtinAlgorithm, string algorithmName);


    public string instance { get; init; }
    public Function[] functions { get; init; }
    public ManagedPathfinding? managedPathfinding { get; init; }

    public float size { get; set; }


    public readonly Function GetFunction(string id)
        => functions.Where(f => f.id == id).First();
}