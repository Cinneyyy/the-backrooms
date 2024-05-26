using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace Backrooms;

public record struct Vec2i(int x, int y) : IEnumerable<int>
{
    public int x = x, y = y;

    public static readonly Vec2i zero = new(0);
    public static readonly Vec2i one = new(1), negOne = -one;
    public static readonly Vec2i min = new(int.MinValue), max = new(int.MaxValue);
    public static readonly Vec2i right = new(1, 0), left = -right;
    public static readonly Vec2i up = new(0, 1), down = -up;
    public static readonly Vec2i[] directions = [up, down, left, right];


    public readonly int sqrLength => x*x + y*y;
    public readonly float length => MathF.Sqrt(sqrLength);


    public Vec2i(int xy) : this(xy, xy) { }


    public int this[int idx]
    {
        readonly get => new int[2] {x, y} [idx];
        set {
            switch(idx)
            {
                case 0: x = value; break;
                case 1: y = value; break;
                default: throw new IndexOutOfRangeException();
            }
        }
    }


    public readonly override string ToString() => $"({x}; {y})";

    public readonly void Deconstruct(out int x, out int y)
    {
        x = this.x;
        y = this.y;
    }

    public readonly IEnumerator<int> GetEnumerator()
    {
        yield return x;
        yield return y;
    }
    readonly IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();


    public static Vec2i Parse(string x, string y)
        => new(int.Parse(x), int.Parse(y));


    public static Vec2i operator +(Vec2i a, Vec2i b) => new(a.x + b.x, a.y + b.y);
    public static Vec2i operator -(Vec2i a, Vec2i b) => new(a.x - b.x, a.y - b.y);
    public static Vec2i operator *(Vec2i a, Vec2i b) => new(a.x * b.x, a.y * b.y);
    public static Vec2i operator /(Vec2i a, Vec2i b) => new(a.x / b.x, a.y / b.y);
    public static Vec2i operator %(Vec2i a, Vec2i b) => new(a.x % b.x, a.y % b.y);

    public static Vec2i operator *(int a, Vec2i b) => new(a * b.x, a * b.y);
    public static Vec2i operator *(Vec2i a, int b) => new(a.x * b, a.y * b);
    public static Vec2i operator /(Vec2i a, int b) => new(a.x / b, a.y / b);
    public static Vec2i operator %(Vec2i a, int b) => new(a.x % b, a.y % b);

    public static Vec2f operator *(float a, Vec2i b) => new(a * b.x, a * b.y);
    public static Vec2f operator *(Vec2i a, float b) => new(a.x * b, a.y * b);
    public static Vec2f operator /(Vec2i a, float b) => new(a.x / b, a.y / b);
    public static Vec2f operator %(Vec2i a, float b) => new(a.x % b, a.y % b);

    public static Vec2i operator +(Vec2i v) => new(+v.x, +v.y);
    public static Vec2i operator -(Vec2i v) => new(-v.x, -v.y);

    public static Vec2i operator &(Vec2i a, Vec2i b) => new(a.x & b.x, a.y & b.y);
    public static Vec2i operator |(Vec2i a, Vec2i b) => new(a.x | b.x, a.y | b.y);
    public static Vec2i operator ^(Vec2i a, Vec2i b) => new(a.x ^ b.x, a.y ^ b.y);
    public static Vec2i operator <<(Vec2i a, Vec2i b) => new(a.x << b.x, a.y << b.y);
    public static Vec2i operator >>(Vec2i a, Vec2i b) => new(a.x >> b.x, a.y >> b.y);

    public static Vec2i operator &(Vec2i a, int b) => new(a.x & b, a.y & b);
    public static Vec2i operator |(Vec2i a, int b) => new(a.x | b, a.y | b);
    public static Vec2i operator ^(Vec2i a, int b) => new(a.x ^ b, a.y ^ b);
    public static Vec2i operator <<(Vec2i a, int b) => new(a.x << b, a.y << b);
    public static Vec2i operator >>(Vec2i a, int b) => new(a.x >> b, a.y >> b);


    public static implicit operator Size(Vec2i v) => new(v.x, v.y);
    public static implicit operator Vec2i(Size s) => new(s.Width, s.Height);
    public static implicit operator Point(Vec2i v) => new(v.x, v.y);
    public static implicit operator Vec2i(Point p) => new(p.X, p.Y);
}