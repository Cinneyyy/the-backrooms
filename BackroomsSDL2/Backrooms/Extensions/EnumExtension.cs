namespace Backrooms.Extensions;

public static class EnumExtension
{
    public static bool IsAir(this Tile tile)
        => ((byte)tile & 1) == 0;
    public static bool IsSolid(this Tile tile)
        => ((byte)tile & 1) == 1;
}