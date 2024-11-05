using Backrooms.Extensions;

namespace Backrooms;

public struct Range(int min, int max, bool maxInclusive = true)
{
    public int min = min, max = max;
    public bool maxInclusive = maxInclusive;


    public readonly int random => maxInclusive ? RNG.RangeIncl(min, max) : RNG.Range(min, max);


    public readonly int this[float t]
        => t.Lerp(min, max);
}