using System;

namespace Backrooms;

public static class RNG
{
    private static Random rand;
    private static readonly object syncLock = new();


    public static int int32
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
}