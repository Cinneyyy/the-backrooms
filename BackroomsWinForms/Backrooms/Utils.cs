﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Net;
using System.Numerics;
using System.Text;
using System.Windows.Forms;
using Backrooms.Coroutines;
using NAudio.Wave;
using System.Text.RegularExpressions;

namespace Backrooms;

public static class Utils
{
    public const float Pi = MathF.PI;
    public const float Tau = MathF.Tau;
    public const float Rad2Deg = 180f/Pi;
    public const float Deg2Rad = Pi/180f;

    public static T Lerp<T>(T a, T b, T t) where T : INumber<T>
        => a + (b - a) * t;
    public static float Lerp(float min, float max, float t)
        => min + (max - min) * t;

    public static float InvLerp(float x, float min, float max)
        => (x - min) / (max - min);

    public static float Map(float x, float fmin, float fmax, float tmin, float tmax)
        => (x - fmin) * (tmax - tmin) / (fmax - fmin) + tmin;

    public static float LerpClamped(float a, float b, float t)
        => Lerp(a, b, Clamp01(t));

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

    public static TOut Pipe<TIn, TOut>(this TIn @in, Func<TIn, TOut> func)
        => func(@in);


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

    public static bool RoughlyEquals(this float a, float b, float eps = 1e-8f)
        => MathF.Abs(a - b) <= eps;
    public static bool RoughlyZero(this float f, float eps = 1e-8f)
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
        => loc.x <= pt.x && loc.y <= pt.y && loc.x + size.x >= pt.x && loc.y + size.y >= pt.y;
    public static bool InsideRectCentered(Vec2f loc, Vec2f size, Vec2f pt)
        => loc.x - size.x/2f <= pt.x && loc.x + size.x/2f >= pt.x && loc.y - size.y/2f <= pt.y && loc.y + size.y/2f >= pt.y;

    public static void Shuffle<T>(this IList<T> list)
    {
        for(int i = list.Count-1; i > 0; i--)
        {
            int k = RNG.RangeIncl(i);
            (list[i], list[k]) = (list[k], list[i]);
        }
    }

    public static Coroutine StartCoroutine(this IEnumerator iterator, Window win)
        => win.StartCoroutine(iterator);

    public static ZipArchiveEntry GetEntry(this ZipArchive zip, string fileName, string[] extensions)
    {
        foreach(ZipArchiveEntry entry in zip.Entries)
            if(entry.Name.StartsWith(fileName, StringComparison.OrdinalIgnoreCase))
                foreach(string ext in extensions)
                    if(entry.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                        return entry;

        return null;
    }

    public static MemoryStream ZipDirectoryInMemory(string dirPath)
    {
        MemoryStream stream = new();
        ZipFile.CreateFromDirectory(dirPath, stream, CompressionLevel.NoCompression, false);
        return stream;
    }

    public static void Populate<T>(this T[] arr, Func<int, T> @new)
    {
        for(int i = 0; i < arr.Length; i++)
            arr[i] = @new(i);
    }
    public static void Populate<T>(this T[,] arr, Func<int, int, T> @new)
    {
        for(int i = 0; i < arr.Length0(); i++)
            for(int j = 0; j < arr.Length1(); j++)
                arr[i, j] = @new(i, j);
    }
    public static void Populate<T>(this T[,,] arr, Func<int, int, int, T> @new)
    {
        for(int i = 0; i < arr.Length0(); i++)
            for(int j = 0; j < arr.Length1(); j++)
                for(int k = 0; k < arr.Length2(); k++)
                    arr[i, j, k] = @new(i, j, k);
    }

    public static T At<T>(this T[] arr, Index x)
        => arr[x];
    public static T At<T>(this T[,] arr, Index x, Index y)
        => arr[x.Value, y.Value];
    public static T At<T>(this T[,,] arr, Index x, Index y, Index z)
        => arr[x.Value, y.Value, z.Value];

    public static float F(this double d) => (float)d;
    public static float F(this int i) => i;
    public static int I(this long l) => (int)l;
    public static int I(this float f) => (int)f;

    public static Keys ToKey(this MouseButtons mb)
        => mb switch {
            MouseButtons.Left => Keys.LButton,
            MouseButtons.Right => Keys.RButton,
            MouseButtons.Middle => Keys.MButton,
            MouseButtons.XButton1 => Keys.XButton1,
            MouseButtons.XButton2 => Keys.XButton2,
            MouseButtons.None => Keys.None,
            _ => throw new($"Invalid mouse button ;; {mb} ({(int)mb})")
        };

    public static bool IsSubclassOfGeneric(this Type type, Type genericType)
    {
        if(type is null || type == typeof(object))
            return false;

        if(type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
            return true;

        return type.BaseType.IsSubclassOfGeneric(genericType);
    }

    public static byte[] ToTwoBytes(this ushort uint16)
        => [(byte)(uint16 >> 8), (byte)(uint16 & 255)];
    public static ushort ToUint16(this byte[] twoBytes)
        => (ushort)((ushort)(twoBytes[0] << 8) | twoBytes[1]);

    public static WaveStream FileToWaveStream(string fileName)
        => Path.GetExtension(fileName).ToLower() switch {
        ".mp3" => new Mp3FileReader(fileName),
        ".wav" => new WaveFileReader(fileName),
        ".aiff" => new AiffFileReader(fileName),
        string f => throw new($"Unsupported audio format: {f}")
    };

    public static WaveStream StreamToWaveStream(Stream stream, string codec)
        => codec.ToLower() switch {
            ".mp3" or "mp3" => new Mp3FileReader(stream),
            ".wav" or "wav" => new WaveFileReader(stream),
            ".aiff" or "aiff" => new AiffFileReader(stream),
            _ => throw new($"Unsupported audio format: {codec}")
        };

    public static Color32 ToColTableValue(this ConsoleColor color)
        => new(color switch {
            ConsoleColor.Black => 0x000000,
            ConsoleColor.DarkBlue => 0x000080,
            ConsoleColor.DarkGreen => 0x008000,
            ConsoleColor.DarkCyan => 0x008080,
            ConsoleColor.DarkRed => 0x800000,
            ConsoleColor.DarkMagenta => 0x800080,
            ConsoleColor.DarkYellow => 0x808000,
            ConsoleColor.Gray => 0xc0c0c0,
            ConsoleColor.DarkGray => 0x808080,
            ConsoleColor.Blue => 0x0000ff,
            ConsoleColor.Green => 0x00ff00,
            ConsoleColor.Cyan => 0x00ffff,
            ConsoleColor.Red => 0xff0000,
            ConsoleColor.Magenta => 0xff00ff,
            ConsoleColor.Yellow => 0xffff00,
            ConsoleColor.White => 0xffffff,
            _ => throw new($"Invalid ConsoleColor ;; {color} ({(int)color})")
        });

    public static bool InBetweenIncl(float x, float min, float max)
        => x <= max && x >= min;
    public static bool InBetweenIncl(int x, int min, int max)
        => x <= max && x >= min;
    public static bool InBetweenExcl(float x, float min, float max)
        => x < max && x > min;
    public static bool InBetweenExcl(int x, int min, int max)
        => x < max && x > min;

    public static int ToTernary(bool negOneCond, bool posOneCond)
        => negOneCond ? -1 : posOneCond ? 1 : 0;
    public static int ToTernary(Input input, Keys negKey, Keys posKey)
        => ToTernary(input.KeyHelt(negKey), input.KeyHelt(posKey));
    public static int ToTernary(Input input, InputAction negAction, InputAction posAction)
        => ToTernary(input.KeyHelt(negAction), input.KeyHelt(posAction));

    public static (byte r, byte g, byte b) BlendColors(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2, float alpha = .5f)
    {
        byte r = (byte)MathF.Sqrt(Lerp(Sqr(r1), Sqr(r2), alpha));
        byte g = (byte)MathF.Sqrt(Lerp(Sqr(g1), Sqr(g2), alpha));
        byte b = (byte)MathF.Sqrt(Lerp(Sqr(b1), Sqr(b2), alpha));
        return (r, g, b);
    }

    public static (byte r, byte g, byte b) BlendColorsCrude(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2, float alpha = .5f)
    {
        byte r = (byte)Lerp(r1, r2, alpha);
        byte g = (byte)Lerp(g1, g2, alpha);
        byte b = (byte)Lerp(b1, b2, alpha);
        return (r, g, b);
    }

    public static bool IsEmpty(this Tile tile)
        => Backrooms.Map.IsEmptyTile(tile);
    public static bool IsColliding(this Tile tile)
        => Backrooms.Map.IsCollidingTile(tile);

    public static string GetLocalIPAddress()
    {
        foreach(IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            if(ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();

        throw new($"No network adapters with an IPv4 address in the system! (host name: {Dns.GetHostName()})");
    }

    public static char? ToChar(this Keys key)
        => (key ^ Keys.Shift ^ Keys.Control) switch {
            >= Keys.A and <= Keys.Z => (char)('a' + key - Keys.A + ((key & Keys.Shift) != 0 ? 'A' - 'a' : 0)),
            >= Keys.D0 and <= Keys.D9 => (char)('0' + key - Keys.D0),
            Keys.Space => ' ',
            Keys.OemPeriod => '.',
            _ => null
        };

    public static T If<T>(this T t, bool predicate, Func<T, T> transform)
        => predicate ? transform(t) : t;

    public static TTarget IfHasValue<TTarget, TNullable>(this TTarget t, TNullable? nullable, Func<TTarget, TNullable, TTarget> transform) where TNullable : struct
        => nullable is TNullable value ? transform(t, value) : t;

    public static string Replace(this string str, Regex match, MatchEvaluator evaluator)
        => match.Replace(str, evaluator);
}