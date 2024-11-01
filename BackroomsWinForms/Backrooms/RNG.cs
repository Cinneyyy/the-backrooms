using System;
using System.Collections.Generic;
using System.Linq;

namespace Backrooms;

public static class RNG
{
    private static Random rand = new();
    private static readonly object syncLock = new();


    public static int signedInt => Range(int.MinValue, int.MaxValue);
    public static int positiveInt => Range(int.MaxValue);
    public static bool coinToss => value > .5f;
    public static float value
    {
        get {
            lock(syncLock)
                return rand.NextSingle();
        }
    }


    public static void SetSeed(int seed)
    {
        lock(syncLock)
            rand = new(seed);
    }

    public static int Range(int minIncl, int maxExcl)
    {
        lock(syncLock)
            return rand.Next(minIncl, maxExcl);
    }
    public static int Range(int maxExcl)
        => Range(0, maxExcl);
    public static float Range(float min, float max)
        => value * (max - min) + min;
    public static float Range(float max)
        => value * max;

    public static int RangeIncl(int min, int max)
        => Range(min, max+1);
    public static int RangeIncl(int max)
        => Range(max+1);

    public static T SelectRandom<T>(this IEnumerable<T> collection)
        => collection.ElementAt(Range(collection.Count()));

    public static bool Chance(float chance = .5f)
        => value <= chance;

    public static Vec2i Vec2i(Vec2i min, Vec2i max)
        => new(Range(min.x, max.x), Range(min.y, max.y));
    public static Vec2i Vec2i(Vec2i max)
        => new(Range(max.x), Range(max.y));
    public static Vec2i Vec2i(int minX, int minY, int maxX, int maxY)
        => new(Range(minX, maxX), Range(minY, maxY));
    public static Vec2i Vec2i(int maxX, int maxY)
        => new(Range(maxX), Range(maxY));

    public static Vec2f Vec2f(Vec2f min, Vec2f max)
        => new(Range(min.x, max.x), Range(min.y, max.y));
    public static Vec2f Vec2f(Vec2f max)
        => new(Range(max.x), Range(max.y));
    public static Vec2f Vec2f(float minX, float minY, float maxX, float maxY)
        => new(Range(minX, maxX), Range(minY, maxY));
    public static Vec2f Vec2f(float maxX, float maxY)
        => new(Range(maxX), Range(maxY));
}