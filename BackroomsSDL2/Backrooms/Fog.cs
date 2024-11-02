using System;

namespace Backrooms;

#pragma warning disable CA2211 // Non-constant fields should not be visible
public static class Fog
{
    public static bool enabled = true;
    public static float maxDist = Camera.DEFAULT_RENDER_DIST * .925f;
    public static float eps = 0.015625f; // 2^-6


    /// <summary> Try to use GetDistFogNormalized when a normalized distance is already available </summary>
    public static float GetDistFog(float dist)
        => enabled ? MathF.Pow(eps, dist / maxDist) : 1f;
    public static float GetDistFogNormalized(float dist01)
        => enabled ? MathF.Pow(eps, dist01) : 1f;
}