using System.Collections.Generic;
using System.Linq;

namespace Backrooms.Pathfinding;

public class BreadthFirstSearch : IPathfindingAlgorithm
{
    public Path FindPath(Map map, Vec2i start, Vec2i end)
    {
        Queue<Vec2i> queue = new([start]);
        Dictionary<Vec2i, Vec2i> visited = new() { [start] = start };

        while(queue.Count > 0)
        {
            Vec2i curr = queue.Dequeue();
            if(curr == end)
                break;


            foreach(Vec2i neighbor in from n in map.GetNeighbors(curr, t => Map.IsEmptyTile(t))
                                      where !visited.ContainsKey(n)
                                      select n)
            {
                queue.Enqueue(neighbor);
                visited[neighbor] = curr;
            }
        }

        List<Vec2i> nodes = [];
        Vec2i node = end;
        while(node != start)
            if(visited.TryGetValue(node, out Vec2i newNode))
            {
                nodes.Add(node);
                node = newNode;
            }

        nodes.Reverse();
        nodes = IPathfindingAlgorithm.ShortenPath(map, nodes);
        return new(nodes);
    }
}