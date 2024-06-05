using System.Linq;

namespace Backrooms;

public readonly record struct EntityTags
{
    public readonly record struct Function(string id, string name);
    public readonly record struct Vector2(float x, float y)
    {
        public static implicit operator Vec2f(Vector2 v) => new(v.x, v.y);
    }


    public string instance { get; init; }
    public bool builtinPathfinding { get; init; }
    public string pathfinding { get; init; }
    public float speed { get; init; }
    public float size { get; init; }
    public Function[] functions { get; init; }


    public Function GetFunction(string id)
        => functions.Where(f => f.id == id).First();
}