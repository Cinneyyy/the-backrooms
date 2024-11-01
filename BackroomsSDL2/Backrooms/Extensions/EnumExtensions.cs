namespace Backrooms.Extensions;

public static class EnumExtensions
{
    public static bool IsAir(this Tile tile)
        => ((byte)tile & 1) == 0;
    public static bool isSolid(this Tile tile)
        => ((byte)tile & 1) == 1;
}