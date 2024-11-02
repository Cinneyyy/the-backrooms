using System;

namespace Backrooms;

public struct FogSettings(float maxDist, bool enabled = true)
{
    public bool enabled = enabled;
    public float maxDist = maxDist;
    public float eps = 0.015625f; // 2^-6


    /// <summary> Try to use GetDistFogNormalized when a normalized distance is already available </summary>
    public readonly float GetDistFog(float dist)
        => enabled ? MathF.Pow(eps, dist / maxDist) : 1f;
    public readonly float GetDistFogNormalized(float dist01)
        => enabled ? MathF.Pow(eps, dist01) : 1f;
}