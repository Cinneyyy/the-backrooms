using System;

namespace Backrooms.Lighting;

public interface ILightDistribution
{
    Vec2i ClosestLightSource(Vec2f pt);
    bool IsLightTile(Vec2i pt);


    public float ClosestLightSqrDist(Vec2f pt)
        => (pt + Vec2f.half - ClosestLightSource(pt)).sqrLength;

    public bool InLightTile(Vec2f pt)
        => IsLightTile(pt.floor);

    public float ComputeLighting(float currBrightness, Vec2f hitPoint)
    {
        float sqrDist = ClosestLightSqrDist(hitPoint);
        return float.Clamp(currBrightness * Fog.GetDistFog(MathF.Sqrt(sqrDist)) * Lights.lightStrength / (1f + sqrDist), Lights.minBrightness, 1f);
    }
}