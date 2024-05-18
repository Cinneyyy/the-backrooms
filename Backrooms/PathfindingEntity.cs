using System.Collections.Generic;
using System.Linq;

namespace Backrooms;

public class PathfindingEntity(Map map, Vec2f pos, float speed, float olafRadius, float maxTravelDist = .2f)
{
    public Map map = map;
    public Vec2f pos = pos;
    public float speed = speed;
    public Vec2f[] path = [];
    public int currPathIdx;
    public float maxTravelDist = maxTravelDist;
    public float olafRadius = olafRadius;


    public Vec2i roundedPos
    {
        get => pos.Round();
        set => pos = value;
    }


    public void RefreshPath(Vec2f target)
    {
        path = FindPath(roundedPos, target.Round());
        currPathIdx = 0;
    }

    public Vec2f[] FindPath(Vec2i start, Vec2i target)
    {
        Queue<Vec2i> queue = [];
        Dictionary<Vec2i, Vec2i> parent = [];

        queue.Enqueue(start);
        parent[start] = start;

        while(queue.Count > 0)
        {
            var current = queue.Dequeue();
            if(current == target)
                break;

            foreach(var neighbor in GetNeighbors(current))
            {
                if(!parent.ContainsKey(neighbor))
                {
                    queue.Enqueue(neighbor);
                    parent[neighbor] = current;
                }
            }
        }

        List<Vec2i> path = [];
        Vec2i node = target;
        while(node != start)
        {
            if(parent.TryGetValue(node, out Vec2i value))
            {
                path.Add(node);
                node = value;
            }
        }
        path.Reverse();

        return (from p in path select p + Vec2f.half).ToArray();
    }

    public void Tick(float dt)
    {
        if(currPathIdx >= path.Length)
            return;

        pos += (path[currPathIdx] - pos).normalized * speed * dt;

        if(Vec2f.RoughlyEqual(pos, path[currPathIdx], olafRadius/2f))
            currPathIdx++;
    }


    private IEnumerable<Vec2i> GetNeighbors(Vec2i cell)
    {
        Vec2i[] deltas = [ Vec2i.left, Vec2i.right, Vec2i.down, Vec2i.up ];

        foreach(Vec2i delta in deltas)
        {
            Vec2i newPos = cell + delta;

            if(map.InBounds(newPos) && Map.IsEmptyTile(map[newPos]))
                yield return newPos;
        }
    }
}