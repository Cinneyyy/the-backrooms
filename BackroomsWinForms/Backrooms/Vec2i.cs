﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace Backrooms;

public record struct Vec2i(int x, int y) : IEnumerable<int>, IVector<Vec2i>
{
    public int x = x, y = y;

    public static readonly Vec2i zero = new(0);
    public static readonly Vec2i one = new(1), negOne = -one;
    public static readonly Vec2i minValue = new(int.MinValue), maxValue = new(int.MaxValue);
    public static readonly Vec2i right = new(1, 0), left = -right;
    public static readonly Vec2i up = new(0, 1), down = -up;
    public static readonly Vec2i[] directions = [up, down, left, right];

    public readonly int sqrLength => x*x + y*y;
    public readonly float sqrLengthF => sqrLength;
    public readonly float length => MathF.Sqrt(sqrLength);
    public readonly int min => Math.Min(x, y);
    public readonly int max => Math.Max(x, y);


    public Vec2i(int xy) : this(xy, xy) { }


    public int this[int idx]
    {
        readonly get => new int[2] { x, y } [idx];
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
    public readonly string ToString(string format) => format.Replace("$x", x.ToString()).Replace("$y", y.ToString());
    public readonly string ToString(string format, string intFormat) => format.Replace("$x", x.ToString(intFormat)).Replace("$y", y.ToString(intFormat));
    public readonly string ToStringIntFormat(string intFormat) => $"({x.ToString(intFormat)}; {y.ToString(intFormat)})";

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

    public void Swap()
        => (x, y) = (y, x);


    public static Vec2i Parse(string x, string y)
        => new(int.Parse(x), int.Parse(y));
    public static Vec2i Parse(string[] compontents)
        => Parse(compontents[0], compontents[1]);


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


    public static explicit operator Size(Vec2i v) => new(v.x, v.y);
    public static explicit operator Vec2i(Size s) => new(s.Width, s.Height);

    public static explicit operator Point(Vec2i v) => new(v.x, v.y);
    public static explicit operator Vec2i(Point p) => new(p.X, p.Y);

    public static explicit operator (int x, int y)(Vec2i v) => (v.x, v.y);
    public static implicit operator Vec2i((int x, int y) t) => new(t.x, t.y);
}