using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Backrooms.Pathfinding;

public struct Path(Vec2f[] points) : IEnumerable<Vec2f>
{
    public readonly Vec2f[] points = points;
    private int _current;


    public int current
    {
        readonly get => _current;
        set => _current = Utils.Clamp(value, 0, points.Length);
    }


    public Path(IEnumerable<Vec2f> points) : this([..points]) { }

    public Path(Vec2i[] points) : this(from p in points select p + Vec2f.half) { }

    public Path(IEnumerable<Vec2i> points) : this(from p in points select p + Vec2f.half) { }


    readonly IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();


    public readonly IEnumerator<Vec2f> GetEnumerator()
    {
        foreach(Vec2f pt in points[current..])
            yield return pt;
    }

    public Vec2f GetNextPoint(Vec2f pos, float marginOfError)
    {
        if(current >= points.Length)
            return pos;

        if(current < points.Length - 1 && (pos - points[current]).length <= marginOfError)
            current++;

        return points[current];
    }
}