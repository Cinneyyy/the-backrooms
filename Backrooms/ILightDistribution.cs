using System;

namespace Backrooms;

public interface ILightDistribution
{
    Vec2i ClosestLightSource(Vec2f pt);
    bool IsLightTile(Vec2i pt);


    public float ClosestSqrLightDist(Vec2f pt)
        => (pt + Vec2f.half - ClosestLightSource(pt)).sqrLength;

    public bool InLightTile(Vec2f pt)
        => IsLightTile(pt.Floor());

    public float ComputeLighting(Renderer rend, float currBrightness, Vec2f hitPoint)
    {
        float sqrDist = ClosestSqrLightDist(hitPoint);
        return Utils.Clamp(currBrightness * rend.GetDistanceFog(MathF.Sqrt(sqrDist)) * rend.lightStrength / (1f + sqrDist), rend.minBrightness, 1f);
    }
}