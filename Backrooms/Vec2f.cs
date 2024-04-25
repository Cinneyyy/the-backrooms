using System;
using System.Collections;
using System.Collections.Generic;

namespace Backrooms;

public record struct Vec2f(float x, float y) : IEnumerable<float>
{
    public float x = x, y = y;

    public static readonly Vec2f zero = new(0f), nan = new(float.NaN);
    public static readonly Vec2f one = new(1f), negOne = -one;
    public static readonly Vec2f half = new(.5f), negHalf = -half;
    public static readonly Vec2f inf = new(float.PositiveInfinity), negInf = new(float.NegativeInfinity);
    public static readonly Vec2f min = new(float.MinValue), max = new(float.MaxValue);
    public static readonly Vec2f right = new(1f, 0f), left = -right;
    public static readonly Vec2f up = new(0f, 1f), down = -up;
    public static readonly Vec2f[] directions = [ up, down, left, right ];


    public readonly float sqrLength => x*x + y*y;
    public readonly float length => MathF.Sqrt(sqrLength);
    public readonly Vec2f normalized => this / length;
    public readonly float toAngle => Utils.NormAngle(MathF.Atan2(y, x));


    public Vec2f(float xy) : this(xy, xy) { }


    public readonly override string ToString() => $"({x}; {y})";
    public readonly string ToString(string fFormat) => $"({x.ToString(fFormat)}; {y.ToString(fFormat)})";

    public readonly void Deconstruct(out float x, out float y)
    {
        x = this.x;
        y = this.y;
    }

    public readonly Vec2i Round() => new((int)x, (int)y);
    public readonly Vec2i Ceil() => new((int)MathF.Ceiling(x), (int)MathF.Ceiling(y));
    public readonly Vec2i Floor() => new((int)MathF.Floor(x), (int)MathF.Floor(y));

    public readonly IEnumerator<float> GetEnumerator()
    {
        yield return x;
        yield return y;
    }
    readonly IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();


    public static Vec2f Op(Vec2f a, Vec2f b, Func<float, float, float> op)
        => new(op(a.x, b.x), op(a.y, b.y));

    public static Vec2f Rotate(Vec2f vec, float radians)
    {
        float sin = MathF.Sin(radians), cos = MathF.Cos(radians);
        return new(
            vec.x * cos - vec.y * sin,
            vec.x * sin + vec.y * cos);
    }

    public static Vec2f FromAngle(float radians)
        => new(MathF.Cos(radians), MathF.Sin(radians));

    public static float Dot(Vec2f a, Vec2f b)
        => a.x*b.x + a.y*b.y;
    public static float Dot(float a, float b)
        => MathF.Cos(a)*MathF.Cos(b) + MathF.Sin(a)*MathF.Sin(b);

    public static Vec2f Lerp(Vec2f a, Vec2f b, float t) 
        => new(Utils.Lerp(a.x, b.x, t), Utils.Lerp(a.y, b.y, t));


    public static Vec2f operator +(Vec2f a, Vec2f b) => new(a.x + b.x, a.y + b.y);
    public static Vec2f operator -(Vec2f a, Vec2f b) => new(a.x - b.x, a.y - b.y);
    public static Vec2f operator *(Vec2f a, Vec2f b) => new(a.x * b.x, a.y * b.y);
    public static Vec2f operator /(Vec2f a, Vec2f b) => new(a.x / b.x, a.y / b.y);
    public static Vec2f operator %(Vec2f a, Vec2f b) => new(a.x % b.x, a.y % b.y);

    public static Vec2f operator *(float a, Vec2f b) => new(a * b.x, a * b.y);
    public static Vec2f operator *(Vec2f a, float b) => new(a.x * b, a.y * b);
    public static Vec2f operator /(Vec2f a, float b) => new(a.x / b, a.y / b);
    public static Vec2f operator %(Vec2f a, float b) => new(a.x % b, a.y % b);

    public static Vec2f operator +(Vec2f v) => new(+v.x, +v.y);
    public static Vec2f operator -(Vec2f v) => new(-v.x, -v.y);


    public static explicit operator Vec2i(Vec2f v) => new((int)v.x, (int)v.y);
    public static implicit operator Vec2f(Vec2i v) => new(v.x, v.y);
}