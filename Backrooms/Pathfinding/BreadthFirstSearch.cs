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


            foreach(Vec2i neighbor in from n in map.GetNeighbors(curr)
                                      where map[n] == Tile.Empty
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

        //if(nodes.Count > 3)
        //    for(int i = 0; i < nodes.Count-2; i++)
        //        for(int j = i+1; j < nodes.Count; j++)
        //        {
        //            bool allEmpty = true;

        //            for(int k = i+1; k < j-1; k++)
        //                if(map[nodes[k]] != Tile.Empty)
        //                    allEmpty = false;

        //            if(allEmpty)
        //            {
        //                nodes.RemoveRange(i+1, j-1);
        //                j -= j - i - 2;
        //            }
        //        }

        nodes.Reverse();
        IPathfindingAlgorithm.ShortenPath(map, nodes);
        Out($"Start: {start} ;; End: {end} ;; Length: {nodes.Count} ;; Nodes: {nodes.FormatStr(", ")}");
        return new(nodes);
    }
}