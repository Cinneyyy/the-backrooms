namespace Backrooms.Lighting;

public struct LightingSettings(ILightDistribution distribution, bool enabled = true)
{
    public bool enabled = enabled;
    public ILightDistribution distribution = distribution;
    public float minBrightness = 0f;
    public float lightStrength = 2f;
}