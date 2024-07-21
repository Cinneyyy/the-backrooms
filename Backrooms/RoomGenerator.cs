using System;
using System.Collections;
using System.Collections.Generic;

namespace Backrooms;

public class RoomGenerator : IEnumerable
{
    public Vec2i gridSize = new(192, 192); // 192, 108
    public float mazeFill = .5f; // .8f
    public int mazeCount = 1000; // 1000
    public float collResolveChance = .5f; // .5;
    public Vec2i roomCount = new(10, 15); // 5, 15
    public Vec2i roomSize = new(8, 25); // 1, 32
    public Vec2i pillarRoomSize = new(20, 50); // 1, 32
    public Vec2i pillarRoomCount = new(10, 15); // 1, 4
    public Vec2i pillarSpacing = new(2, 4); // 2, 6
    public int staggeredPillarRoomChance = 50; // 50
    public Vec2i staggeredPillarStep = new(1, 4);
    public int[] frontierAttempts = [1]; // [1]
    public List<(Vec2i loc, Vec2i size)> rooms = [], pillarRooms = [];

    private Random rand = new();
    private int columns, rows;
    private Tile[,] tiles;


    public int seed { get; private set; }


    public Tile this[int x, int y]
    {
        get => tiles[y, x];
        set => tiles[y, x] = value;
    }


    public void Initiate(int seed = 0)
    {
        rand = new(seed);
        this.seed = seed;

        rooms.Clear();
        pillarRooms.Clear();

        tiles = new Tile[rows = gridSize.y, columns = gridSize.x];
        for(int x = 0; x < gridSize.x; x++)
            for(int y = 0; y < gridSize.y; y++)
                this[x, y] = Tile.Wall;
    }

    public void GenerateHallways()
    {
        HashSet<Vec2i> visited = [];

        for(int i = 0; i < mazeCount; i++)
        {
            Vec2i startCell = new(Rand(columns), Rand(rows));
            visited.Add(startCell);
            List<Vec2i> frontier = [ startCell ];

            while((float)visited.Count / (columns * rows) < mazeFill)
            {
                if(frontier is [])
                    break;

                int idx = Rand(frontier.Count);
                Vec2i cell = frontier[idx];
                frontier.RemoveAt(idx);

                visited.Add(cell);
                this[cell.x, cell.y] = Tile.Air;

                List<Vec2i> neighbors = [];
                Vec2i c;

                if(cell.x > 1 && !visited.Contains(c = new(cell.x - 2, cell.y)))
                    neighbors.Add(c);
                if(cell.x < columns - 2 && !visited.Contains(c = new(cell.x + 2, cell.y)))
                    neighbors.Add(c);
                if(cell.y > 1 && !visited.Contains(c = new(cell.x, cell.y - 2)))
                    neighbors.Add(c);
                if(cell.y < rows - 2 && !visited.Contains(c = new(cell.x, cell.y + 2)))
                    neighbors.Add(c);

                int attempts = frontierAttempts[Rand(frontierAttempts.Length)];
                for(int j = 0; j < attempts && neighbors is not []; j++)
                {
                    Vec2i nextCell = neighbors[Rand(neighbors.Count)];
                    if(rand.NextDouble() > collResolveChance || Map.IsCollidingTile(this[(cell.x + nextCell.x) / 2, (cell.y + nextCell.y) / 2]))
                    {
                        frontier.Add(nextCell);
                        this[(cell.x + nextCell.x) / 2, (cell.y + nextCell.y) / 2] = Tile.Air;
                    }
                    neighbors.Remove(nextCell);
                }
            }
        }
    }

    public void GenerateRooms()
    {
        int roomCount = Rand(this.roomCount.x, this.roomCount.y, true);

        for(int i = 0; i < roomCount; i++)
        {
            Vec2i size = new(Rand(roomSize.x, roomSize.y, true), Rand(roomSize.x, roomSize.y, true));
            Vec2i loc = new(Rand(columns - size.x), Rand(rows - size.y));
            rooms.Add((new(loc.x, loc.y), new(size.x, size.y)));

            for(int y = loc.y; y < loc.y + size.y; y++)
                for(int x = loc.x; x < loc.x + size.x; x++)
                    this[x, y] = Tile.BigRoomAir;
        }
    }

    public void GeneratePillarRooms()
    {
        int roomCount = Rand(pillarRoomCount.x, pillarRoomCount.y, true);

        for(int i = 0; i < roomCount; i++)
        {
            Vec2i size = new(Rand(pillarRoomSize.x, pillarRoomSize.y, true), Rand(pillarRoomSize.x, pillarRoomSize.y, true));
            Vec2i loc = new(Rand(columns - size.x), Rand(rows - size.y));
            pillarRooms.Add((new(loc.x, loc.y), new(size.x, size.y)));

            for(int y = loc.y; y < loc.y + size.y; y++)
                for(int x = loc.x; x < loc.x + size.x; x++)
                    this[x, y] = Tile.PillarRoomAir;

            if(Rand(100) >= staggeredPillarRoomChance) // staggered
            {
                bool inRoom(int x, int y) => x >= 0 && y >= 0 && x < size.x && y < size.y;

                int hStep = Rand(staggeredPillarStep.x, staggeredPillarStep.y, true), vStep = Rand(staggeredPillarStep.x, staggeredPillarStep.y, true);
                if(hStep == vStep && hStep == 1)
                    if(Rand(100) >= 50) hStep = 2;
                    else vStep = 2;

                for(int x = -size.y; x < size.x;)
                {
                    int nextX = x + 2*hStep;

                    for(int y = 0; y < size.y; y += vStep)
                    {
                        if(inRoom(x, y))
                            this[x + loc.x, y + loc.y] = Tile.Pillar;
                        x += hStep;
                    }

                    x = nextX;
                }
            }
            else
            {
                int spacing = Rand(pillarSpacing.x, pillarSpacing.y, true);
                for(int x = 0; x < size.x; x += spacing)
                    for(int y = 0; y < size.y; y += spacing)
                        this[x + loc.x, y + loc.y] = Tile.Pillar;
            }
        }
    }

    public Tile[,] FormatTiles()
    {
        Tile[,] ret = new Tile[tiles.Length1(), tiles.Length0()];

        for(int x = 0; x < ret.Length0(); x++)
            for(int y = 0; y < ret.Length1(); y++)
                ret[x, y] = this[x, y];

        return ret;
    }

    public IEnumerator GetEnumerator() => tiles.GetEnumerator();


    private int Rand(int max, bool incl = false) => rand.Next(max + (incl ? 1 : 0));
    private int Rand(int min, int max, bool incl = false) => rand.Next(min, max + (incl ? 1 : 0));
}