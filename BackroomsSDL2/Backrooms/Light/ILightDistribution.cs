using System;

namespace Backrooms.Light;

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
        return float.Clamp(currBrightness * Fog.GetDistFog(MathF.Sqrt(sqrDist)) * Lighting.lightStrength / (1f + sqrDist), Lighting.minBrightness, 1f);
    }

    public (float r, float g, float b) ComputeLightingRgb((float r, float g, float b) curr, (float r, float g, float b) light, Vec2f hitPoint)
    {
        float sqrDist = ClosestLightSqrDist(hitPoint);
        float coeff = Fog.GetDistFog(MathF.Sqrt(sqrDist)) * Lighting.lightStrength / (1f + sqrDist);
        return (
            curr.r * coeff * light.r,
            curr.g * coeff * light.g,
            curr.b * coeff * light.b
        );
    }
}