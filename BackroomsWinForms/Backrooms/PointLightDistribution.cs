using System.Collections.Generic;

namespace Backrooms;

public class PointLightDistribution(Vec2i size) : ILightDistribution
{
    private readonly HashSet<Vec2i> lightSources = [];
    private Vec2i[,] bakedDistances = new Vec2i[size.x, size.y];
    private Vec2i size = size;


    public Vec2i ClosestLightSource(Vec2f pt)
        => bakedDistances[pt.x.Floor(), pt.y.Floor()];

    public bool IsLightTile(Vec2i pt)
        => lightSources.Contains(pt);

    public void Resize(Vec2i newSize, bool bake = true)
    {
        bakedDistances = new Vec2i[newSize.y, newSize.x];

        if(bake)
            Bake();
    }

    public void AddLightSource(Vec2i pt, bool bake = true)
    {
        if(lightSources.Add(pt) && bake)
            Bake();
    }

    public void AddLightSources(IEnumerable<Vec2i> pts, bool bake = true)
    {
        foreach(Vec2i pt in pts)
            AddLightSource(pt, false);

        if(bake)
            Bake();
    }


    private void Bake()
    {
        Vec2i[] ptArr = [..lightSources];

        Vec2i getClosest(Vec2i pt)
        {
            int closestDist = int.MaxValue;
            Vec2i closestPt = Vec2i.negOne;

            foreach(Vec2i p in ptArr)
                if((p - pt).sqrLength is int dist && dist < closestDist)
                {
                    closestDist = dist;
                    closestPt = p;
                }

            return closestPt;
        }

        for(int x = 0; x < size.x; x++)
            for(int y = 0; y < size.y; y++)
                bakedDistances[x, y] = getClosest(new(x, y));
    }
}