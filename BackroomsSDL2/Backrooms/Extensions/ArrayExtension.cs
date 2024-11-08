using System;

namespace Backrooms.Extensions;

public static class ArrayExtension
{
    public static int SizeX<T>(this T[,] arr2d)
        => arr2d.GetLength(0);
    public static int SizeY<T>(this T[,] arr2d)
        => arr2d.GetLength(1);
    public static Vec2i Size<T>(this T[,] arr2d)
        => new(arr2d.GetLength(0), arr2d.GetLength(1));

    public static int SizeX<T>(this T[,,] arr3d)
        => arr3d.GetLength(0);
    public static int SizeY<T>(this T[,,] arr3d)
        => arr3d.GetLength(1);
    public static int SizeZ<T>(this T[,,] arr3d)
        => arr3d.GetLength(2);

    public static void Populate<T>(this T[,] arr2d, T value)
    {
        int w = arr2d.GetLength(0), h = arr2d.GetLength(1);

        for(int x = 0; x < w; x++)
            for(int y = 0; y < h; y++)
                arr2d[x, y] = value;
    }
    public static void Populate<T>(this T[,] arr2d, Func<int, int, T> map)
    {
        int w = arr2d.GetLength(0), h = arr2d.GetLength(1);

        for(int x = 0; x < w; x++)
            for(int y = 0; y < h; y++)
                arr2d[x, y] = map(x, y);
    }
}