using System.Collections.Generic;
using Backrooms.Extensions;

namespace Backrooms.MapGeneration;

public class SeededGenerator : IGenerator<SeededGenerator.Settings>
{
    public readonly record struct Settings(int seed, int hallwayCount, float hallwayFill, float hallwayCollResolveChance, Range roomCount, Range roomSize, Range pillarRoomCount, Range pillarRoomSize, Range pillarSpacing, float staggeredPillarChance, Range staggeredPillarStep, int frontierAttempts, bool sealOff)
    {
        public static readonly Settings defaultSettings = new(0, 1000, .5f, .6f, new(10, 20), new(10, 30), new(10, 20), new(20, 60), new(2, 4), .5f, new(2, 4), 1, true);
    }


    private Settings settings;
    private Vec2i size;
    private Tile[,] tiles;


    public Tile[,] Generate(Vec2i size, Settings settings)
    {
        this.settings = settings;
        this.size = size;
        tiles = new Tile[size.x, size.y];

        for(int x = 0; x < size.x; x++)
            for(int y = 0; y < size.y; y++)
                tiles[x, y] = Tile.Wall;

        RNG.SetSeed(settings.seed);

        GenerateHallways(settings.hallwayCount);
        GenerateRooms(settings.roomCount.random);
        GeneratePillarRooms(settings.pillarRoomCount.random);

        if(settings.sealOff)
        {
            for(int x = 0; x < size.x; x++)
            {
                tiles[x, 0] = Tile.Wall;
                tiles[x, size.y-1] = Tile.Wall;
            }

            for(int y = 0; y < size.y; y++)
            {
                tiles[0, y] = Tile.Wall;
                tiles[size.x-1, y] = Tile.Wall;
            }
        }

        return tiles;
    }


    private void GenerateHallways(int count)
    {
        HashSet<Vec2i> visited = [];
        float targetTiles = settings.hallwayFill * (size.x * size.y);

        for(int i = 0; i < count; i++)
        {
            Vec2i startCell = new(RNG.Range(size.x), RNG.Range(size.y));
            visited.Add(startCell);
            List<Vec2i> frontier = [startCell];

            while(visited.Count < targetTiles)
            {
                if(frontier is [])
                    break;

                int index = RNG.Range(frontier.Count);
                Vec2i cell = frontier[index];
                frontier.RemoveAt(index);

                visited.Add(cell);
                tiles[cell.x, cell.y] = Tile.Air;

                List<Vec2i> neighbors = [];
                Vec2i n;

                if(cell.x > 1 && !visited.Contains(n = new(cell.x - 2, cell.y))) neighbors.Add(n);
                if(cell.y > 1 && !visited.Contains(n = new(cell.x, cell.y - 2))) neighbors.Add(n);
                if(cell.x < size.x-2 && !visited.Contains(n = new(cell.x + 2, cell.y))) neighbors.Add(n);
                if(cell.y < size.y-2 && !visited.Contains(n = new(cell.x, cell.y + 2))) neighbors.Add(n);

                for(int j = 0; j < settings.frontierAttempts && neighbors is not []; j++)
                {
                    Vec2i nextCell = neighbors.SelectRandom();
                    Vec2i avg = (cell + nextCell) / 2;

                    if(RNG.Chance(settings.hallwayCollResolveChance) || tiles[avg.x, avg.y].IsSolid())
                    {
                        frontier.Add(nextCell);
                        tiles[avg.x, avg.y] = Tile.Air;
                    }

                    neighbors.Remove(nextCell);
                }
            }
        }
    }

    private void GenerateRooms(int count)
    {
        for(int i = 0; i < count; i++)
        {
            Vec2i size = new(settings.roomSize.random, settings.roomSize.random);
            Vec2i loc = new(RNG.Range(this.size.x - size.x), RNG.Range(this.size.y - size.y));

            for(int x = 0; x < size.x; x++)
                for(int y = 0; y < size.y; y++)
                    tiles[x + loc.x, y + loc.y] = Tile.BigRoomAir;
        }
    }

    private void GeneratePillarRooms(int count)
    {
        for(int i = 0; i < count; i++)
        {
            Vec2i size = new(settings.roomSize.random, settings.roomSize.random);
            Vec2i loc = new(RNG.Range(this.size.x - size.x), RNG.Range(this.size.y - size.y));

            for(int x = 0; x < size.x; x++)
                for(int y = 0; y < size.y; y++)
                    tiles[x + loc.x, y + loc.y] = Tile.PillarRoomAir;

            if(RNG.Chance(settings.staggeredPillarChance))
            {
                Vec2i step = new(settings.staggeredPillarStep.random, settings.staggeredPillarStep.random);

                if(step.x == step.y && step.x == 1)
                    if(RNG.coinToss) step.x = 2;
                    else step.y = 2;

                // tf did i do here
                for(int x = -size.y; x < size.x; x += 2 * step.x)
                    for(int y = 0, _x = x; y < size.y; y += step.y, _x += step.x)
                        if(_x >= 0 && y >= 0 && _x < size.x && y < size.y)
                            tiles[_x + loc.x, y + loc.y] = Tile.Pillar;
            }
            else
            {
                int spacing = settings.pillarSpacing.random;

                for(int x = 0; x < size.x; x += spacing)
                    for(int y = 0; y < size.y; y += spacing)
                        tiles[x + loc.x, y + loc.y] = Tile.Pillar;
            }
        }
    }
}