using System.Collections.Generic;
using System.Linq;

namespace Backrooms.Pathfinding;

public interface IPathfindingAlgorithm
{
    Path FindPath(Map map, Vec2i start, Vec2i end);

    static List<Vec2i> ShortenPath(Map map, List<Vec2i> nodes)
    {
        //// TODO: fix this fix this fix this
        //return nodes;

        if(nodes.Count <= 2)
            return nodes;

        bool lineOfSight(int a, int b)
        {
            for(int i = a; i < b; i++)
                if(Map.IsCollidingTile(map[nodes[i]]))
                    return false;

            return true;
        }

        List<Vec2i> clearances = [];

        for(int i = 0; i < nodes.Count-2; i++)
            for(int j = i+2; j < nodes.Count-1; j++)
                if(lineOfSight(i, j))
                    clearances.Add(new(i+1, j-1));

        clearances.Sort((a, b) => (a.y - a.x) - (b.y - b.x));

        HashSet<int> removedIndices = [];
        foreach(Vec2i range in clearances)
        {
            IEnumerable<int> affectedIndices = Enumerable.Range(range.x, range.y);
            bool valid = true;

            foreach(int i in affectedIndices)
                if(removedIndices.Contains(i))
                {
                    valid = false;
                    break;
                }

            if(!valid)
                continue;

            foreach(int i in affectedIndices)
                removedIndices.Add(i);
        }

        List<Vec2i> filteredNodes = [];
        for(int i = 0; i < nodes.Count; i++)
            if(!removedIndices.Contains(i))
                filteredNodes.Add(nodes[i]);
        return filteredNodes;
    }
}