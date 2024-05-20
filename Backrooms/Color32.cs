using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace Backrooms;

public struct Color32(byte r, byte g, byte b, byte a = 0xff)
{
    public byte r = r, g = g, b = b, a = a;

    public static readonly Color32 white = new(0xff, 0xff, 0xff);
    public static readonly Color32 black = new(0, 0, 0);


    public readonly float rf => r / 255f;
    public readonly float gf => g / 255f;
    public readonly float bf => b / 255f;
    public readonly float af => a / 255f;


    public Color32(float r, float g, float b, float a = 1f) : this((byte)(r * 255f), (byte)(g * 255f), (byte)(b * 255f), (byte)(a * 255f)) { }

    public Color32(float v, float a) : this((byte)(v * 255f), (byte)(a * 255f)) { }

    public Color32(float v) : this((byte)(v * 255f)) { }

    public Color32(byte v, byte a) : this(v, v, v, a) { }

    public Color32(byte v) : this(v, v, v, v) { }


    public readonly override bool Equals([NotNullWhen(true)] object obj) => base.Equals(obj);
    public readonly override int GetHashCode() => base.GetHashCode();


    public static unsafe Color32 FromData24(byte* scan)
        => new(*(scan+2), (*scan+1), *scan);


    public static Color32 operator +(Color32 a, Color32 b) => new(a.r + b.r, a.g + b.g, a.b + b.b);
    public static Color32 operator -(Color32 a, Color32 b) => new(a.r - b.r, a.g - b.g, a.b - b.b);
    public static Color32 operator *(Color32 a, Color32 b) => new(a.rf * b.rf, a.gf * b.gf, a.bf * b.bf);
    public static Color32 operator /(Color32 a, Color32 b) => new(a.rf / b.rf, a.gf / b.gf, a.bf / b.bf);
    public static Color32 operator %(Color32 a, Color32 b) => new(a.rf % b.rf, a.gf % b.gf, a.bf % b.bf);
    public static Color32 operator *(Color32 a, float b) => new(a.rf * b, a.gf * b, a.bf * b);
    public static Color32 operator /(Color32 a, float b) => new(a.rf / b, a.gf / b, a.bf / b);
    public static Color32 operator %(Color32 a, float b) => new(a.rf % b, a.gf % b, a.bf % b);

    public static bool operator ==(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
    public static bool operator !=(Color32 a, Color32 b) => !(a == b);


    public static explicit operator Color(Color32 col32) => Color.FromArgb(col32.a, col32.r, col32.g, col32.b);
}