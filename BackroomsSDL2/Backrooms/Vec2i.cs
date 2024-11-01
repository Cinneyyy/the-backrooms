using System;

namespace Backrooms;

public struct Vec2i(int x, int y)
{
    public Vec2i(int xy) : this(xy, xy) { }


    public int x = x, y = y;

    public static readonly Vec2i zero = new(0), one = new(1);
    public static readonly Vec2i up = new(0, 1), down = -up, right = new(1, 0), left = -right;


    public readonly Vec2f asVec2f => new(this);


    public readonly override bool Equals(object obj)
        => obj is Vec2i v && v == this;

    public readonly override int GetHashCode()
        => HashCode.Combine(x, y);

    public readonly override string ToString()
        => $"({x}, {y})";


    // Vec2i x Vec2i
    public static Vec2i operator +(Vec2i a, Vec2i b) => new(a.x + b.x, a.y + b.y);
    public static Vec2i operator -(Vec2i a, Vec2i b) => new(a.x - b.x, a.y - b.y);
    public static Vec2i operator *(Vec2i a, Vec2i b) => new(a.x * b.x, a.y * b.y);
    public static Vec2i operator /(Vec2i a, Vec2i b) => new(a.x / b.x, a.y / b.y);
    public static Vec2i operator %(Vec2i a, Vec2i b) => new(a.x % b.x, a.y % b.y);
    public static Vec2i operator &(Vec2i a, Vec2i b) => new(a.x & b.x, a.y & b.y);
    public static Vec2i operator |(Vec2i a, Vec2i b) => new(a.x | b.x, a.y | b.y);
    public static Vec2i operator ^(Vec2i a, Vec2i b) => new(a.x ^ b.x, a.y ^ b.y);

    // Vec2i x int
    public static Vec2i operator +(Vec2i v, int n) => new(v.x + n, v.y + n);
    public static Vec2i operator -(Vec2i v, int n) => new(v.x - n, v.y - n);
    public static Vec2i operator *(Vec2i v, int n) => new(v.x * n, v.y * n);
    public static Vec2i operator /(Vec2i v, int n) => new(v.x / n, v.y / n);
    public static Vec2i operator %(Vec2i v, int n) => new(v.x % n, v.y % n);
    public static Vec2i operator &(Vec2i v, int n) => new(v.x & n, v.y & n);
    public static Vec2i operator |(Vec2i v, int n) => new(v.x | n, v.y | n);
    public static Vec2i operator ^(Vec2i v, int n) => new(v.x ^ n, v.y ^ n);

    // Vec2i x float
    public static Vec2f operator +(Vec2i v, float n) => new(v.x + n, v.y + n);
    public static Vec2f operator -(Vec2i v, float n) => new(v.x - n, v.y - n);
    public static Vec2f operator *(Vec2i v, float n) => new(v.x * n, v.y * n);
    public static Vec2f operator /(Vec2i v, float n) => new(v.x / n, v.y / n);
    public static Vec2f operator %(Vec2i v, float n) => new(v.x % n, v.y % n);

    // int x Vec2i
    public static Vec2i operator +(int n, Vec2i v) => new(n + v.x, n + v.y);
    public static Vec2i operator -(int n, Vec2i v) => new(n - v.x, n - v.y);
    public static Vec2i operator *(int n, Vec2i v) => new(n * v.x, n * v.y);
    public static Vec2i operator /(int n, Vec2i v) => new(n / v.x, n / v.y);
    public static Vec2i operator %(int n, Vec2i v) => new(n % v.x, n % v.y);
    public static Vec2i operator &(int n, Vec2i v) => new(n & v.x, n & v.y);
    public static Vec2i operator |(int n, Vec2i v) => new(n | v.x, n | v.y);
    public static Vec2i operator ^(int n, Vec2i v) => new(n ^ v.x, n ^ v.y);

    // float x Vec2i
    public static Vec2f operator +(float n, Vec2i v) => new(n + v.x, n + v.y);
    public static Vec2f operator -(float n, Vec2i v) => new(n - v.x, n - v.y);
    public static Vec2f operator *(float n, Vec2i v) => new(n * v.x, n * v.y);
    public static Vec2f operator /(float n, Vec2i v) => new(n / v.x, n / v.y);
    public static Vec2f operator %(float n, Vec2i v) => new(n % v.x, n % v.y);

    // Bit-shift
    public static Vec2i operator <<(Vec2i a, Vec2i b) => new(a.x << b.x, a.y << b.y);
    public static Vec2i operator >>(Vec2i a, Vec2i b) => new(a.x >> b.x, a.y >> b.y);
    public static Vec2i operator <<(Vec2i v, int n) => new(v.x << n, v.y << n);
    public static Vec2i operator >>(Vec2i v, int n) => new(v.x >> n, v.y >> n);

    // Unary
    public static Vec2i operator -(Vec2i v) => new(-v.x, -v.y);
    public static Vec2i operator +(Vec2i v) => new(+v.x, +v.y);
    public static Vec2i operator ~(Vec2i v) => new(~v.x, ~v.y);

    public static bool operator ==(Vec2i a, Vec2i b) => a.x == b.x && a.y == b.y;
    public static bool operator !=(Vec2i a, Vec2i b) => a.x != b.x || a.y != b.y;
}