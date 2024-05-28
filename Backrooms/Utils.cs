using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Backrooms.Gui;

namespace Backrooms;

public static class Utils
{
    public const float Pi = MathF.PI;
    public const float Tau = MathF.Tau;
    public const float Rad2Deg = 180f/Pi;
    public const float Deg2Rad = Pi/180f;


    public static T Lerp<T>(T a, T b, T t) where T : INumber<T>
        => Clamp(LerpUnclamped(a, b, t), T.Zero, T.One);
    public static float Lerp(float a, float b, float t)
        => Clamp(LerpUnclamped(a, b, t), 0f, 1f);

    public static T LerpUnclamped<T>(T a, T b, T t) where T : INumber<T>
        => a + (b - a) * t;
    public static float LerpUnclamped(float a, float b, float t)
        => a + (b - a) * t;

    public static int Length<T>(this T[,] arr2d, int idx)
        => arr2d.GetLength(idx);
    public static int Length0<T>(this T[,] arr2d)
        => arr2d.Length(0);
    public static int Length1<T>(this T[,] arr2d)
        => arr2d.Length(1);

    public static int Length<T>(this T[,,] arr3d, int idx)
        => arr3d.GetLength(idx);
    public static int Length0<T>(this T[,,] arr3d)
        => arr3d.Length(0);
    public static int Length1<T>(this T[,,] arr3d)
        => arr3d.Length(1);
    public static int Length2<T>(this T[,,] arr3d)
        => arr3d.Length(2);

    public static T Clamp<T>(T a, T min, T max) where T : INumber<T>
        => a <= min ? min : a >= max ? max : a;
    public static float Clamp(float a, float min, float max)
        => a <= min ? min : a >= max ? max : a;
    public static int Clamp(int a, int min, int max)
        => a <= min ? min : a >= max ? max : a;

    public static float Clamp01(float f)
        => Clamp(f, 0f, 1f);

    public static void ResizeArr2D<T>(ref T[,] arr, int newX, int newY)
    {
        T[,] res = new T[newX, newY];

        for(int x = 0; x < arr.Length0(); x++)
            for(int y = 0; y < arr.Length1(); y++)
                res[x, y] = arr[x, y];

        arr = res;
    }

    public static T Mod<T>(T x, T y) where T : INumber<T>
        => (x%y + y) % y;
    public static float Mod(float x, float y)
        => (x%y + y) % y;

    public static float NormAngle(float a)
        => Mod(a, Tau);

    public static bool RoughlyEqual(float a, float b, float eps = 1e-8f)
        => MathF.Abs(a - b) <= eps;
    public static bool RoughlyZero(float f, float eps = 1e-8f)
        => MathF.Abs(f) <= eps;

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

    public static T Sqr<T>(T x) where T : INumber<T>
        => x*x;
    public static float Sqr(float x)
        => x*x;
    public static int Sqr(int x)
        => x*x;

    public static T Cube<T>(T x) where T : INumber<T>
        => x*x*x;
    public static float Cube(float x)
        => x*x*x;
    public static int Cube(int x)
        => x*x*x;

    public static int Round(this float f)
        => (int)MathF.Round(f);
    public static int Floor(this float f)
        => (int)f;
    public static int Ceil(this float f)
        => (int)MathF.Ceiling(f);

    public static bool InsideRect(Vec2f loc, Vec2f size, Vec2f pt)
        => pt.x > loc.x - size.x/2f && pt.x < loc.x + size.x/2f && pt.y > loc.y - size.y/2f && pt.y < loc.y + size.y/2f;

    public static void Shuffle<T>(this IList<T> list, Random rand)
    {
        for(int i = list.Count-1; i > 0; i--)
        {
            int k = rand.Next(i + 1);
            (list[i], list[k]) = (list[k], list[i]);
        }
    }

    public static Vec2f ToVec2f(this Dir dir)
        => Vec2f.FromAngle(dir.ToAngle());

    public static float ToAngle(this Dir dir)
        => dir switch {
            Dir.North => 90f,
            Dir.South => 270f,
            Dir.East => 0f,
            Dir.West => 180f,
            Dir.NE => 45f,
            Dir.NW => 90f + 45f,
            Dir.SW => 180f + 45f,
            Dir.SE => 270f + 45f,
            _ => throw new("Invalid dirction")
        } * Deg2Rad;
}