using System;

namespace Backrooms.Extensions;

public static class MathExtension
{
    public const float Pi = MathF.PI;
    public const float Tau = MathF.Tau;
    public const float Rad2Deg = 180f/Pi;
    public const float Deg2Rad = Pi/180f;


    public static int Floor(this float f) => (int)MathF.Floor(f);
    public static int Ceil(this float f) => (int)MathF.Ceiling(f);
    public static int Round(this float f) => (int)MathF.Round(f);

    public static float Mod(this float x, float y)
        => (x%y + y) % y;

    public static float NormAngle(this float angle)
        => Mod(angle, Tau);

    public static float ToRad(this float degAngle)
        => degAngle * Deg2Rad;
    public static float ToDeg(this float radAngle)
        => radAngle * Rad2Deg;

    public static uint Lerp(this float t, uint min, uint max)
        => (uint)(min + (max - min) * t);
    public static byte Lerp(this float t, byte min, byte max)
        => (byte)(min + (max - min) * t);
    public static int Lerp(this float t, int min, int max)
        => (int)(min + (max - min) * t);

    public static float Sqr(this float f)
        => f*f;
    public static uint Sqr(this uint i)
        => i*i;
    public static int Sqr(this int i)
        => i*i;
    public static int Sqr(this byte b)
        => b*b;
}