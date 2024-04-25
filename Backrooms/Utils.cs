using System;

namespace Backrooms;

public static class Utils
{
    public const float Pi = MathF.PI;
    public const float Tau = MathF.Tau;
    public const float Rad2Deg = 180f/Pi;
    public const float Deg2Rad = Pi/180f;


    public static float Lerp(float a, float b, float t)
        => Clamp(a + (b - a) * t, 0f, 1f);

    public static float LerpUnclamped(float a, float b, float t)
        => a + (b - a) * t;

    public static float Clamp(float a, float min, float max)
        => a <= min ? min : a >= max ? max : a;
    public static int Clamp(int a, int min, int max)
        => a <= min ? min : a >= max ? max : a;

    public static void ResizeArr2D<T>(ref T[,] arr, int newX, int newY)
    {
        T[,] res = new T[newX, newY];

        for(int x = 0; x < arr.GetLength(0); x++)
            for(int y = 0; y < arr.GetLength(1); y++)
                res[x, y] = arr[x, y];

        arr = res;
    }

    public static float Mod(float x, float y)
        => (x%y + y) % y;

    public static float NormAngle(float a)
        => Mod(a, Tau);

    public static bool RoughlyEqual(float a, float b, float eps = 1e-5f)
        => MathF.Abs(a - b) <= eps;

    public static void DoNothing() { }
}