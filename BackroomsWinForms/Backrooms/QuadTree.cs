using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Backrooms;

public class QuadTree(Vec2i size, int maxDepth) : IEnumerable<Vec2i>
{
    private class Node(Vec2i value) : IEnumerable<Node>
    {
        public Vec2i point = value;
        public Node child0, child1, child2, child3;


        public Node this[int index]
        {
            get => index switch {
                0 => child0,
                1 => child1,
                2 => child2,
                3 => child3,
                _ => throw new IndexOutOfRangeException()
            };
            set {
                switch(index)
                {
                    case 0: child0 = value; break;
                    case 1: child1 = value; break;
                    case 2: child2 = value; break;
                    case 3: child3 = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }


        public IEnumerator<Node> GetEnumerator()
        {
            for(int i = 0; i < 4; i++)
                if(this[i] is Node child and not null)
                    foreach(Node subChild in child)
                        yield return subChild;
        }


        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }


    private readonly int maxDepth = maxDepth;
    private readonly Node root = new(size/2);
    private readonly Vec2i size = size;


    public (Vec2i pt, float sqrDist)? FindNearest(Vec2f pt)
        => FindNearest(root, pt, Vec2i.zero, size);

    public void Add(Vec2i pt)
        => Add(root, pt, Vec2i.zero, size, 0);

    public IEnumerator<Vec2i> GetEnumerator()
        => root.Select(n => n.point).GetEnumerator();


    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    private void Add(Node node, Vec2i pt, Vec2i min, Vec2i max, int depth)
    {
        if(depth >= maxDepth)
            return;

        Vec2i mid = (min + max) / 2;
        int index = (pt.x < mid.x ? 0 : 1) + (pt.y < mid.y ? 0 : 2);

        if(node[index] is null)
            node[index] = new(pt);
        else
        {
            bool idxDivByTwo = (index & 1) == 0;
            Add(
                node[index], pt,
                new(idxDivByTwo ? min.x : mid.x, index < 2 ? min.y : mid.y),
                new(idxDivByTwo ? mid.x : max.x, index < 2 ? mid.y : max.y), depth + 1);
        }
    }


    private static (Vec2i pt, float sqrDist)? FindNearest(Node node, Vec2f pt, Vec2i min, Vec2i max)
    {
        if(node is null)
            return null;

        Vec2i mid = (min + max) / 2;

        int index = (pt.x < mid.x ? 0 : 1) + (pt.y < mid.y ? 0 : 2);
        Vec2i? nearest = node.point;
        float nearestSqrDist = (pt - node.point).sqrLength;

        for(int i = 0; i < 4; i++)
        {
            if(node[i] is null)
                continue;

            bool idxDivByTwo = (index & 1) == 0;
            (Vec2i pt, float sqrDist)? child = FindNearest(
                node[index], pt,
                new(idxDivByTwo ? min.x : mid.x, index < 2 ? min.y : mid.y),
                new(idxDivByTwo ? mid.x : max.x, index < 2 ? mid.y : max.y));

            if(child is not null && child.Value.sqrDist < nearestSqrDist)
            {
                nearest = child.Value.pt;
                nearestSqrDist = child.Value.sqrDist;
            }
        }

        return nearest is null ? null : (nearest.Value, nearestSqrDist);
    }
}