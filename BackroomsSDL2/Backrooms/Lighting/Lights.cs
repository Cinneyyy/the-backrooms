namespace Backrooms.Lighting;

#pragma warning disable CA2211 // Non-constant fields should not be visible
public static class Lights
{
    public static bool enabled = true;
    public static ILightDistribution distribution = new GridLightDistribution(10);
    public static float minBrightness = 0f;
    public static float lightStrength = 2f;
}