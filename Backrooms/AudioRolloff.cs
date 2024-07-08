namespace Backrooms;

public static class AudioRolloff
{
    public static float GetVolume(float dist, float minDist, float rolloffScale)
        => Utils.Clamp01(minDist / (1f + rolloffScale * (dist - 1f)));
    public static float GetVolume(float dist, float minDist)
        => Utils.Clamp01(minDist / (minDist * (1f - dist) + dist));

    public static float GetVolumeSqr(float dist, float minDist, float rolloffScale)
        => Utils.Sqr(GetVolume(dist, minDist, rolloffScale));
    public static float GetVolumeSqr(float dist, float minDist)
        => Utils.Sqr(GetVolume(dist, minDist));

    public static float ForcedFalloff(float dist, float volume, float begin, float end)
        => volume * Utils.Clamp01((dist - end) / (begin - end));
}