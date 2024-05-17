namespace Backrooms.Pathfinding;

public class Pathfinder(Map map, IPathfindingAlgorithm pathfinding)
{
    public Path path;
    public Map map = map;
    public IPathfindingAlgorithm pathfinding = pathfinding;


    public Vec2f MoveTowards(Vec2f pos, float radius, float speed, float dt)
    {
        if(path.points is null || path.points.Length == 0)
            return pos;

        Vec2f nextPt = path.GetNextPoint(pos, .5f - radius);
        return pos + (nextPt - pos).normalized * speed * dt;
    }

    public void FindPath(Vec2f start, Vec2f end)
        => path = pathfinding.FindPath(map, start.Floor(), end.Floor());
}