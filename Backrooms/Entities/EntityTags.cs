namespace Backrooms;

public readonly record struct EntityTags
{
    public readonly record struct Vector2(float x, float y)
    {
        public static implicit operator Vec2f(Vector2 v) => new(v.x, v.y);
    }

    public readonly record struct ManagedPathfinding(string algorithmName, float speed);


    public string instance { get; init; }
    public ManagedPathfinding? managedPathfinding { get; init; }
    public Vector2 size { get; init; }
    public string sprite { get; init; }
    public string audio { get; init; }
    public bool manageSprRendPos { get; init; }
    public bool manageAudioVol { get; init; }
}