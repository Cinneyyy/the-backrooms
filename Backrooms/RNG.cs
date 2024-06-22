using System;
using System.Collections.Generic;
using System.Linq;

namespace Backrooms;

public static class RNG
{
    private static Random rand;
    private static readonly object syncLock = new();


    public static int integer
    {
        get {
            lock(syncLock)
                return rand.Next();
        }
    }
    public static float value
    {
        get {
            lock(syncLock)
                return rand.NextSingle();
        }
    }
    public static bool coinToss => value > .5f;


    static RNG() => rand = new();


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

    public static T SelectRandom<T>(this IEnumerable<T> collection)
        => collection.ElementAt(Range(collection.Count()));
}