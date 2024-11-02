using System;

namespace Backrooms.Extensions;

public static class ColorExtension
{
    public static uint BlendColors(uint a, uint b, float alpha = .5f)
        => JoinColor(
            (byte)MathF.Sqrt(alpha.Lerp(a.R().Sqr(), b.R().Sqr())),
            (byte)MathF.Sqrt(alpha.Lerp(a.G().Sqr(), b.G().Sqr())),
            (byte)MathF.Sqrt(alpha.Lerp(a.B().Sqr(), b.B().Sqr())),
            (byte)MathF.Sqrt(alpha.Lerp(a.A().Sqr(), b.A().Sqr())));

    public static uint BlendColorsCrude(uint a, uint b, float alpha = .5f)
        => JoinColor(
            alpha.Lerp(a.R(), b.R()),
            alpha.Lerp(a.G(), b.G()),
            alpha.Lerp(a.B(), b.B()),
            alpha.Lerp(a.A(), b.A()));

    public static uint JoinColor(byte r, byte g, byte b, byte a)
        => ((uint)r << 24) | ((uint)g << 16) | ((uint)b << 8) | a;
    public static uint JoinColor(float r, float g, float b, float a)
        => ((uint)r << 24) | ((uint)g << 16) | ((uint)b << 8) | (uint)a;

    public static uint MultiplyColor(this uint col, float fac)
        => JoinColor(col.R() * fac, col.G() * fac, col.B() * fac, col.A() * fac);

    public static byte R(this uint col) => (byte)(col >> 24);
    public static byte G(this uint col) => (byte)(col >> 16);
    public static byte B(this uint col) => (byte)(col >> 8);
    public static byte A(this uint col) => (byte)col;
}