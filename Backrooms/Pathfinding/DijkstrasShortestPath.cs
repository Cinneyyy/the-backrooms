using System.Collections.Generic;
using System.Linq;

namespace Backrooms.Pathfinding;

// TODO: fix, not working properly for some reason
public class DijkstrasShortestPath : IPathfindingAlgorithm
{
    public Path FindPath(Map map, Vec2i start, Vec2i end)
    {
        PriorityQueue<Vec2i> queue = [];
        Dictionary<Vec2i, int> dist = [];
        Dictionary<Vec2i, Vec2i> parentMap = [];

        foreach(Vec2i pos in map)
            dist[pos] = int.MaxValue;

        dist[start] = 0;
        queue.Enqueue(start, 0);

        while(queue.Count > 0)
        {
            Vec2i curr = queue.Dequeue();

            if(curr == end)
                break;

            foreach(Vec2i neighbor in from n in map.GetNeighbors(curr, t => t == Tile.Empty)
                                      where !parentMap.ContainsKey(n)
                                      select n)
            {
                int newDist = dist[curr] + 1;

                if(newDist < dist[neighbor])
                {
                    dist[neighbor] = newDist;
                    parentMap[neighbor] = curr;
                    queue.Enqueue(neighbor, newDist);
                }
            }
        }

        List<Vec2i> nodes = [];
        Vec2i currNode = end;

        while(currNode != start)
        {
            nodes.Add(currNode);
            currNode = parentMap[currNode];
        }

        nodes.Add(start);
        nodes.Reverse();

        nodes = IPathfindingAlgorithm.ShortenPath(map, nodes);

        return new(nodes);
    }
}