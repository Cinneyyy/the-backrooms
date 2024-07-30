using System;

namespace Backrooms;

public interface ILightDistribution
{
    float ClosestSqrLightDist(Vec2f pt);
    bool IsInLightTile(Vec2f pt);


    public float ComputeLighting(Renderer rend, float currBrightness, Vec2f hitPoint)
    {
        float sqrDist = ClosestSqrLightDist(hitPoint);
        return Utils.Clamp(currBrightness * rend.GetDistanceFog(MathF.Sqrt(sqrDist)) * rend.lightStrength / (1f + sqrDist), rend.minBrightness, 1f);
    }
}