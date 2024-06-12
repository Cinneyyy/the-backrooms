using System.Linq;

namespace Backrooms;

public record struct EntityTags
{
    public record struct Function(string id, string name);
    public record struct Vector2(float x, float y)
    {
        public static implicit operator Vec2f(Vector2 v) => new(v.x, v.y);
    }


    public string instance { get; set; }
    public bool builtinPathfinding { get; set; }
    public string pathfinding { get; set; }
    public bool automaticallyManagePathfinding { get; set; }
    public float speed { get; set; }
    public float size { get; set; }
    public Function[] functions { get; set; }


    public readonly Function GetFunction(string id)
        => functions.Where(f => f.id == id).First();
}