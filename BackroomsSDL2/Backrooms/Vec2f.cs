using System;
using Backrooms.Extensions;

namespace Backrooms;

public struct Vec2f(float x, float y)
{
    public Vec2f(float xy) : this(xy, xy) { }

    public Vec2f(Vec2i v2i) : this(v2i.x, v2i.y) { }


    public float x = x, y = y;

    public static readonly Vec2f zero = new(0f), one = new(1f);
    public static readonly Vec2f up = new(0f, 1f), down = -up, right = new(1f, 0f), left = -right;


    public readonly float sqrLength => x*x + y*y;
    public readonly float length => MathF.Sqrt(x*x + y*y);
    public readonly Vec2f normalized => sqrLength == 0f ? zero : this / length;
    public readonly float toAngle => MathF.Atan2(y, x);
    public readonly float min => MathF.Min(x, y);
    public readonly float max => MathF.Max(x, y);
    public readonly Vec2i floor => new(x.Floor(), y.Floor());
    public readonly Vec2i ceil => new(x.Ceil(), y.Ceil());
    public readonly Vec2i round => new(x.Round(), y.Round());


    public readonly override bool Equals(object obj)
        => obj is Vec2f v && v == this;

    public readonly override int GetHashCode()
        => HashCode.Combine(x, y);

    public readonly override string ToString()
        => $"({x}, {y})";

    public readonly void Deconstruct(out float x, out float y)
    {
        x = this.x;
        y = this.y;
    }


    public static Vec2f PlaneFromFov(Vec2f dir, float fov)
        => new Vec2f(-dir.y, dir.x) * MathF.Tan(fov/2f);
    public static Vec2f PlaneFromFov(float angle, float fov)
        => PlaneFromFov(FromAngle(angle), fov);

    public static Vec2f PlaneFromFovFactor(Vec2f dir, float fovFactor)
        => new Vec2f(-dir.y, dir.x) * fovFactor;
    public static Vec2f PlaneFromFovFactor(float angle, float fovFactor)
        => PlaneFromFovFactor(FromAngle(angle), fovFactor);

    public static Vec2f FromAngle(float radians)
        => new(MathF.Cos(radians), MathF.Sin(radians));

    public static float Dot(Vec2f a, Vec2f b)
        => a.x*b.x + a.y*b.y;
    public static float Dot(float angleA, float angleB)
        => MathF.Cos(angleA)*MathF.Cos(angleB) + MathF.Sin(angleA)*MathF.Sin(angleB);

    public static float NormalizedDot(Vec2f a, Vec2f b)
        => Dot(a.normalized, b.normalized);

    public static Vec2f Lerp(Vec2f min, Vec2f max, float t)
        => new(float.Lerp(min.x, max.x, t), float.Lerp(min.y, max.y, t));


    // Vec2f x Vec2f
    public static Vec2f operator +(Vec2f a, Vec2f b) => new(a.x + b.x, a.y + b.y);
    public static Vec2f operator -(Vec2f a, Vec2f b) => new(a.x - b.x, a.y - b.y);
    public static Vec2f operator *(Vec2f a, Vec2f b) => new(a.x * b.x, a.y * b.y);
    public static Vec2f operator /(Vec2f a, Vec2f b) => new(a.x / b.x, a.y / b.y);
    public static Vec2f operator %(Vec2f a, Vec2f b) => new(a.x % b.x, a.y % b.y);

    // Vec2f x Vec2i
    public static Vec2f operator +(Vec2f a, Vec2i b) => new(a.x + b.x, a.y + b.y);
    public static Vec2f operator -(Vec2f a, Vec2i b) => new(a.x - b.x, a.y - b.y);
    public static Vec2f operator *(Vec2f a, Vec2i b) => new(a.x * b.x, a.y * b.y);
    public static Vec2f operator /(Vec2f a, Vec2i b) => new(a.x / b.x, a.y / b.y);
    public static Vec2f operator %(Vec2f a, Vec2i b) => new(a.x % b.x, a.y % b.y);

    // Vec2i x Vec2f
    public static Vec2f operator +(Vec2i a, Vec2f b) => new(a.x + b.x, a.y + b.y);
    public static Vec2f operator -(Vec2i a, Vec2f b) => new(a.x - b.x, a.y - b.y);
    public static Vec2f operator *(Vec2i a, Vec2f b) => new(a.x * b.x, a.y * b.y);
    public static Vec2f operator /(Vec2i a, Vec2f b) => new(a.x / b.x, a.y / b.y);
    public static Vec2f operator %(Vec2i a, Vec2f b) => new(a.x % b.x, a.y % b.y);

    // Vec2f x float
    public static Vec2f operator +(Vec2f v, float n) => new(v.x + n, v.y + n);
    public static Vec2f operator -(Vec2f v, float n) => new(v.x - n, v.y - n);
    public static Vec2f operator *(Vec2f v, float n) => new(v.x * n, v.y * n);
    public static Vec2f operator /(Vec2f v, float n) => new(v.x / n, v.y / n);
    public static Vec2f operator %(Vec2f v, float n) => new(v.x % n, v.y % n);

    // Vec2f x int
    public static Vec2f operator +(Vec2f v, int n) => new(v.x + n, v.y + n);
    public static Vec2f operator -(Vec2f v, int n) => new(v.x - n, v.y - n);
    public static Vec2f operator *(Vec2f v, int n) => new(v.x * n, v.y * n);
    public static Vec2f operator /(Vec2f v, int n) => new(v.x / n, v.y / n);
    public static Vec2f operator %(Vec2f v, int n) => new(v.x % n, v.y % n);

    // float x Vec2f
    public static Vec2f operator +(float n, Vec2f v) => new(n + v.x, n + v.y);
    public static Vec2f operator -(float n, Vec2f v) => new(n - v.x, n - v.y);
    public static Vec2f operator *(float n, Vec2f v) => new(n * v.x, n * v.y);
    public static Vec2f operator /(float n, Vec2f v) => new(n / v.x, n / v.y);
    public static Vec2f operator %(float n, Vec2f v) => new(n % v.x, n % v.y);

    // int x Vec2f
    public static Vec2f operator +(int n, Vec2f v) => new(n + v.x, n + v.y);
    public static Vec2f operator -(int n, Vec2f v) => new(n - v.x, n - v.y);
    public static Vec2f operator *(int n, Vec2f v) => new(n * v.x, n * v.y);
    public static Vec2f operator /(int n, Vec2f v) => new(n / v.x, n / v.y);
    public static Vec2f operator %(int n, Vec2f v) => new(n % v.x, n % v.y);

    // Unary
    public static Vec2f operator -(Vec2f v) => new(-v.x, -v.y);
    public static Vec2f operator +(Vec2f v) => new(+v.x, +v.y);

    public static bool operator ==(Vec2f a, Vec2f b) => a.x == b.x && a.y == b.y;
    public static bool operator !=(Vec2f a, Vec2f b) => a.x != b.x || a.y != b.y;


    public static explicit operator Vec2i(Vec2f v) => v.floor;
    public static explicit operator Vec2f(Vec2i v) => v.asVec2f;
}