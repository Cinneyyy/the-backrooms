using System;
using System.Collections;
using System.Collections.Generic;

namespace Backrooms;

public record struct Vec3f(float x, float y, float z) : IEnumerable<float>
{
    public float x = x, y = y, z = z;

    public static readonly Vec3f zero = new(0f), nan = new(float.NaN);
    public static readonly Vec3f one = new(1f), negOne = -one;
    public static readonly Vec3f inf = new(float.PositiveInfinity), negInf = new(float.NegativeInfinity);
    public static readonly Vec3f min = new(float.MinValue), max = new(float.MaxValue);
    public static readonly Vec3f right = new(1f, 0f, 0f), left = -right;
    public static readonly Vec3f up = new(0f, 1f, 0f), down = -up;
    public static readonly Vec3f forward = new(0f, 0f, 1f), backward = -forward;
    public static readonly Vec3f[] directions = [ up, down, left, right, forward, backward ];


    public readonly float sqrLength => x*x + y*y + z*z;
    public readonly float length => MathF.Sqrt(sqrLength);
    public readonly Vec3f normalized => this / length;


    public Vec3f(float x, float y) : this(x, y, 0f) { }
    public Vec3f(float xyz) : this(xyz, xyz, xyz) { }


    public readonly override string ToString() => $"({x}; {y}; {z})";

    public readonly void Deconstruct(out float x, out float y, out float z)
    {
        x = this.x;
        y = this.y;
        z = this.z;
    }

    public readonly IEnumerator<float> GetEnumerator()
    {
        yield return x;
        yield return y;
        yield return z;
    }
    readonly IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public readonly Vec3i Round() => new((int)x, (int)y, (int)z);
    public readonly Vec3i Ceil() => new((int)MathF.Ceiling(x), (int)MathF.Ceiling(y), (int)MathF.Ceiling(z));
    public readonly Vec3i Floor() => new((int)MathF.Floor(x), (int)MathF.Floor(y), (int)MathF.Floor(z));


    public static Vec3f Op(Vec3f a, Vec3f b, Func<float, float, float> op)
        => new(op(a.x, b.x), op(a.y, b.y), op(a.z, b.z));

    public static float Dot(Vec3f a, Vec3f b)
        => a.x*b.x + a.y*b.y + a.z*b.z;

    public static Vec3f Cross(Vec3f a, Vec3f b)
        => new(a.y*b.z - a.z*b.y,
               a.z*b.x - a.x*b.z,
               a.x*b.y - a.y*b.x);

    public static Vec3f RotateX(Vec3f v, float rot)
        => new(v.x,
               MathF.Cos(rot) * v.y - MathF.Sin(rot) * v.z,
               MathF.Sin(rot) * v.y + MathF.Cos(rot) * v.z);

    public static Vec3f RotateY(Vec3f v, float rot)
        => new(MathF.Cos(rot) * v.x - MathF.Sin(rot) * v.z,
               v.y,
               MathF.Sin(rot) * v.x + MathF.Cos(rot) * v.z);

    public static Vec3f RotateZ(Vec3f v, float rot)
        => new(MathF.Cos(rot) * v.x - MathF.Sin(rot) * v.y,
               MathF.Sin(rot) * v.x + MathF.Cos(rot) * v.y,
               v.z);

    public static Vec3f RotateYXZ(Vec3f v, Vec3f rot)
        => RotateZ(RotateX(RotateY(v, rot.y), rot.x), rot.z).normalized;


    public static Vec3f operator +(Vec3f a, Vec3f b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
    public static Vec3f operator -(Vec3f a, Vec3f b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
    public static Vec3f operator *(Vec3f a, Vec3f b) => new(a.x * b.x, a.y * b.y, a.z * b.z);
    public static Vec3f operator /(Vec3f a, Vec3f b) => new(a.x / b.x, a.y / b.y, a.z / b.z);
    public static Vec3f operator %(Vec3f a, Vec3f b) => new(a.x % b.x, a.y % b.y, a.z % b.z);

    public static Vec3f operator *(float a, Vec3f b) => new(a * b.x, a * b.y, a * b.z);
    public static Vec3f operator *(Vec3f a, float b) => new(a.x * b, a.y * b, a.z * b);
    public static Vec3f operator /(Vec3f a, float b) => new(a.x / b, a.y / b, a.z / b);
    public static Vec3f operator %(Vec3f a, float b) => new(a.x % b, a.y % b, a.z % b);

    public static Vec3f operator +(Vec3f v) => new(+v.x, +v.y, +v.z);
    public static Vec3f operator -(Vec3f v) => new(-v.x, -v.y, -v.z);


    public static explicit operator Vec2f(Vec3f v) => new(v.x, v.y);
    public static explicit operator Vec3f(Vec2f v) => new(v.x, v.y);

    public static explicit operator Vec2i(Vec3f v) => new((int)v.x, (int)v.y);
    public static explicit operator Vec3f(Vec2i v) => new(v.x, v.y);
}