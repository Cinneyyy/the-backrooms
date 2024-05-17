using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

    public static float Clamp01(float f)
        => Clamp(f, 0f, 1f);

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

    public static bool RoughlyEqual(float a, float b, float eps = 1e-8f)
        => MathF.Abs(a - b) <= eps;
    public static bool RoughlyZero(float f, float eps = 1e-8f)
        => MathF.Abs(f) < eps;

    public static void DoNothing() { }

    public static Vec2f ReadVec2f(this BinaryReader reader)
        => new(reader.ReadSingle(), reader.ReadSingle());

    public static Vec2i ReadVec2i(this BinaryReader reader)
        => new(reader.ReadInt32(), reader.ReadInt32());

    public static void Write(this BinaryWriter writer, Vec2f v)
    {
        writer.Write(v.x);
        writer.Write(v.y);
    }
    public static void Write(this BinaryWriter writer, Vec2i v)
    {
        writer.Write(v.x);
        writer.Write(v.y);
    }

    public static string FormatStr<T>(this IEnumerable<T> tlist, string seperator, Func<T, string> toString)
    {
        StringBuilder sb = new();

        foreach(T t in tlist)
        {
            sb.Append(toString(t));
            sb.Append(seperator);
        }

        if(sb.Length != 0)
            sb.Remove(sb.Length - seperator.Length, seperator.Length);

        return sb.ToString();
    }
    public static string FormatStr<T>(this IEnumerable<T> tlist, string seperator)
        => tlist.FormatStr(seperator, t => t.ToString());
}