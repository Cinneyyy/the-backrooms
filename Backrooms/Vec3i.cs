using System;
using System.Collections;
using System.Collections.Generic;

namespace Backrooms;

public record struct Vec3i(int x, int y, int z) : IEnumerable<int>
{
    public int x = x, y = y, z = z;

    public static readonly Vec3i zero = new(0);
    public static readonly Vec3i one = new(1), negOne = -one;
    public static readonly Vec3i min = new(int.MinValue), max = new(int.MaxValue);
    public static readonly Vec3i right = new(1, 0, 0), left = -right;
    public static readonly Vec3i up = new(0, 1, 0), down = -up;
    public static readonly Vec3i forward = new(0, 0, 1), backward = -forward;
    public static readonly Vec3i[] directions = [ up, down, left, right, forward, backward ];


    public readonly int sqrLength => x*x + y*y + z*z;
    public readonly float length => MathF.Sqrt(sqrLength);


    public Vec3i(int x, int y) : this(x, y, 0) { }
    public Vec3i(int xyz) : this(xyz, xyz, xyz) { }


    public readonly override string ToString() => $"({x}; {y}; {z})";

    public readonly void Deconstruct(out int x, out int y, out int z)
    {
        x = this.x;
        y = this.y;
        z = this.z;
    }

    public readonly IEnumerator<int> GetEnumerator()
    {
        yield return x;
        yield return y;
        yield return z;
    }
    readonly IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();


    public static Vec3i Op(Vec3i a, Vec3i b, Func<int, int, int> op)
        => new(op(a.x, b.x), op(a.y, b.y), op(a.z, b.z));


    public static Vec3i operator +(Vec3i a, Vec3i b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
    public static Vec3i operator -(Vec3i a, Vec3i b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
    public static Vec3i operator *(Vec3i a, Vec3i b) => new(a.x * b.x, a.y * b.y, a.z * b.z);
    public static Vec3i operator /(Vec3i a, Vec3i b) => new(a.x / b.x, a.y / b.y, a.z / b.z);
    public static Vec3i operator %(Vec3i a, Vec3i b) => new(a.x % b.x, a.y % b.y, a.z % b.z);

    public static Vec3i operator *(int a, Vec3i b) => new(a * b.x, a * b.y, a * b.z);
    public static Vec3i operator *(Vec3i a, int b) => new(a.x * b, a.y * b, a.z * b);
    public static Vec3i operator /(Vec3i a, int b) => new(a.x / b, a.y / b, a.z / b);
    public static Vec3i operator %(Vec3i a, int b) => new(a.x % b, a.y % b, a.z % b);

    public static Vec3i operator +(Vec3i v) => new(+v.x, +v.y, +v.z);
    public static Vec3i operator -(Vec3i v) => new(-v.x, -v.y, -v.z);
}