namespace Backrooms.MapGeneration;

public interface IGenerator<TSettings> where TSettings : struct
{
    Tile[,] Generate(Vec2i size, TSettings settings);
}