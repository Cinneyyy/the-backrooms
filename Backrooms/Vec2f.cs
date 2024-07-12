using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace Backrooms;

public record struct Vec2f(float x, float y) : IEnumerable<float>, IVector<Vec2f>
{
    public static readonly Vec2f zero = new(0f), nan = new(float.NaN);
    public static readonly Vec2f one = new(1f), negOne = -one;
    public static readonly Vec2f half = new(.5f), negHalf = -half;
    public static readonly Vec2f inf = new(float.PositiveInfinity), negInf = new(float.NegativeInfinity);
    public static readonly Vec2f minValue = new(float.MinValue), maxValue = new(float.MaxValue);
    public static readonly Vec2f right = new(1f, 0f), left = -right;
    public static readonly Vec2f up = new(0f, 1f), down = -up;
    public static readonly Vec2f[] directions = [ up, down, left, right ];


    public readonly float sqrLength => x*x + y*y;
    public readonly float length => MathF.Sqrt(sqrLength);
    public readonly Vec2f normalized => sqrLength == 0f ? zero : this / length;
    public readonly float toAngle => MathF.Atan2(y, x);
    public readonly float min => MathF.Min(x, y);
    public readonly float max => MathF.Max(x, y);
    public readonly float xyRatio => x/y;
    public readonly float yxRatio => y/x;
    public readonly float xyDelta => x-y;
    public readonly float yxDelta => y-x;
    public readonly Vec2f swap => new(y, x);


    public Vec2f(float xy) : this(xy, xy) { }


    public readonly override string ToString() => $"({x}; {y})";
    public readonly string ToString(string format) => format.Replace("$x", x.ToString()).Replace("$y", y.ToString());
    public readonly string ToString(string format, string floatFormat) => format.Replace("$x", x.ToString(floatFormat)).Replace("$y", y.ToString(floatFormat));
    public readonly string ToStringFloatFormat(string floatFormat) => $"({x.ToString(floatFormat)}; {y.ToString(floatFormat)})";

    public readonly void Deconstruct(out float x, out float y)
    {
        x = this.x;
        y = this.y;
    }

    public void Normalize() 
        => this = normalized;

    public void Swap()
        => (x, y) = (y, x);

    public readonly Vec2i Round() 
        => new(x.Round(), y.Round());
    public readonly Vec2i Ceil() 
        => new(x.Ceil(), y.Ceil());
    public readonly Vec2i Floor() 
        => new(x.Floor(), y.Floor());

    public void Rotate(float radians)
        => this = Rotate(this, radians);

    public readonly IEnumerator<float> GetEnumerator()
    {
        yield return x;
        yield return y;
    }
    readonly IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();


    public static Vec2f PlaneFromFov(Vec2f dir, float fov)
        => new Vec2f(-dir.y, dir.x) * MathF.Tan(fov/2f);    
    public static Vec2f PlaneFromFov(float angle, float fov)
        => PlaneFromFov(FromAngle(angle), fov);

    public static Vec2f PlaneFromFovFactor(Vec2f dir, float fovFactor)
        => new Vec2f(-dir.y, dir.x) * fovFactor;
    public static Vec2f PlaneFromFovFactor(float angle, float fovFactor)
        => PlaneFromFovFactor(FromAngle(angle), fovFactor);

    public static float Dist(Vec2f a, Vec2f b)
        => (a - b).length;

    public static Vec2f Rotate(Vec2f vec, float radians)
    {
        float sin = MathF.Sin(radians), cos = MathF.Cos(radians);
        return new(
            vec.x * cos - vec.y * sin,
            vec.x * sin + vec.y * cos);
    }

    public static bool RoughlyEqual(Vec2f a, Vec2f b, float eps = 1e-8f)
        => Utils.RoughlyEquals(a.x, b.x, eps) && Utils.RoughlyEquals(a.y, b.y, eps);

    public static Vec2f FromAngle(float radians)
        => new(MathF.Cos(radians), MathF.Sin(radians));

    public static float Dot(Vec2f a, Vec2f b)
        => a.x*b.x + a.y*b.y;
    public static float Dot(float a, float b)
        => MathF.Cos(a)*MathF.Cos(b) + MathF.Sin(a)*MathF.Sin(b);

    public static Vec2f Lerp(Vec2f min, Vec2f max, float t) 
        => new(Utils.Lerp(min.x, max.x, t), Utils.Lerp(min.y, max.y, t));

    public static Vec2f Parse(string x, string y)
        => new(float.Parse(x), float.Parse(y));
    public static Vec2f Parse(string[] components)
        => Parse(components[0], components[1]);


    public static Vec2f operator +(Vec2f a, Vec2f b) => new(a.x + b.x, a.y + b.y);
    public static Vec2f operator -(Vec2f a, Vec2f b) => new(a.x - b.x, a.y - b.y);
    public static Vec2f operator *(Vec2f a, Vec2f b) => new(a.x * b.x, a.y * b.y);
    public static Vec2f operator /(Vec2f a, Vec2f b) => new(a.x / b.x, a.y / b.y);
    public static Vec2f operator %(Vec2f a, Vec2f b) => new(a.x % b.x, a.y % b.y);

    public static Vec2f operator *(float a, Vec2f b) => new(a * b.x, a * b.y);
    public static Vec2f operator /(float a, Vec2f b) => new(a / b.x, a / b.y);
    public static Vec2f operator %(float a, Vec2f b) => new(a % b.x, a % b.y);
    public static Vec2f operator *(Vec2f a, float b) => new(a.x * b, a.y * b);
    public static Vec2f operator /(Vec2f a, float b) => new(a.x / b, a.y / b);
    public static Vec2f operator %(Vec2f a, float b) => new(a.x % b, a.y % b);

    public static Vec2f operator +(Vec2f v) => new(+v.x, +v.y);
    public static Vec2f operator -(Vec2f v) => new(-v.x, -v.y);


    public static explicit operator Vec2i(Vec2f v) => v.Round();
    public static implicit operator Vec2f(Vec2i v) => new(v.x, v.y);

    public static explicit operator SizeF(Vec2f v) => new(v.x, v.y);
    public static explicit operator Vec2f(SizeF s) => new(s.Width, s.Height);

    public static explicit operator PointF(Vec2f v) => new(v.x, v.y);
    public static explicit operator Vec2f(PointF p) => new(p.X, p.Y);

    public static explicit operator (float x, float y)(Vec2f v) => (v.x, v.y);
    public static implicit operator Vec2f((float x, float y) t) => new(t.x, t.y);
}