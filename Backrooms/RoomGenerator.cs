using System;
using System.Collections;
using System.Collections.Generic;

namespace Backrooms;

public class RoomGenerator : IEnumerable
{
    public Vec2i gridSize = new(192*2, 108*2); // 192, 108
    public float mazeFill = .5f; // .8f
    public int mazeCount = 1000; // 1000
    public float collResolveChance = .5f; // .5;
    public (int min, int max) roomCount = (10, 15); // 5, 15
    public (int min, int max) roomSize = (8, 25); // 1, 32
    public (int min, int max) pillarRoomSize = (20, 50); // 1, 32
    public (int min, int max) pillarRoomCount = (10, 15); // 1, 4
    public (int min, int max) pillarSpacing = (2, 4); // 2, 6
    public int staggeredPillarRoomChance = 50; // 50
    public (int min, int max) staggeredPillarStep = (1, 4);
    public int[] frontierAttempts = [ 1 ]; // [ 1 ]
    public List<(Vec2i loc, Vec2i size)> rooms = [], pillarRooms = [];

    private Random rand = new();
    private int columns, rows;
    private Tile[,] tiles;


    public Tile this[int x, int y]
    {
        get => tiles[y, x];
        set => tiles[y, x] = value;
    }


    public void Initiate(int seed = 0)
    {
        rand = new(seed);

        rooms.Clear();
        pillarRooms.Clear();

        tiles = new Tile[rows = gridSize.y, columns = gridSize.x];
        for(int x = 0; x < gridSize.x; x++)
            for(int y = 0; y < gridSize.y; y++)
                this[x, y] = Tile.Wall;
    }

    public void GenerateHallways()
    {
        HashSet<(int x, int y)> visited = [];

        for(int i = 0; i < mazeCount; i++)
        {
            (int x, int y) startCell = (Rand(columns), Rand(rows));
            visited.Add(startCell);
            List<(int x, int y)> frontier = [ startCell ];

            while((float)visited.Count / (columns * rows) < mazeFill)
            {
                if(frontier is [])
                    break;

                int idx = Rand(frontier.Count);
                (int x, int y) cell = frontier[idx];
                frontier.RemoveAt(idx);

                visited.Add(cell);
                this[cell.x, cell.y] = Tile.Empty;

                List<(int x, int y)> neighbors = [];
                (int x, int y) c;

                if(cell.x > 1 && !visited.Contains(c = (cell.x - 2, cell.y)))
                    neighbors.Add(c);
                if(cell.x < columns - 2 && !visited.Contains(c = (cell.x + 2, cell.y)))
                    neighbors.Add(c);
                if(cell.y > 1 && !visited.Contains(c = (cell.x, cell.y - 2)))
                    neighbors.Add(c);
                if(cell.y < rows - 2 && !visited.Contains(c = (cell.x, cell.y + 2)))
                    neighbors.Add(c);

                int attempts = frontierAttempts[Rand(frontierAttempts.Length)];
                for(int j = 0; j < attempts && neighbors is not []; j++)
                {
                    (int x, int y) nextCell = neighbors[Rand(neighbors.Count)];
                    if(rand.NextDouble() > collResolveChance || this[(cell.x + nextCell.x) / 2, (cell.y + nextCell.y) / 2] != Tile.Empty)
                    {
                        frontier.Add(nextCell);
                        this[(cell.x + nextCell.x) / 2, (cell.y + nextCell.y) / 2] = Tile.Empty;
                    }
                    neighbors.Remove(nextCell);
                }
            }
        }
    }

    public void GenerateRooms()
    {
        int roomCount = Rand(this.roomCount.min, this.roomCount.max, true);

        for(int i = 0; i < roomCount; i++)
        {
            (int w, int h) size = (Rand(roomSize.min, roomSize.max, true), Rand(roomSize.min, roomSize.max, true));
            (int x, int y) loc = (Rand(columns - size.w), Rand(rows - size.h));
            rooms.Add((new(loc.x, loc.y), new(size.w, size.h)));

            for(int y = loc.y; y < loc.y + size.h; y++)
                for(int x = loc.x; x < loc.x + size.w; x++)
                    this[x, y] = Tile.Empty;
        }
    }

    public void GeneratePillarRooms()
    {
        int roomCount = Rand(pillarRoomCount.min, pillarRoomCount.max, true);

        for(int i = 0; i < roomCount; i++)
        {
            (int w, int h) size = (Rand(pillarRoomSize.min, pillarRoomSize.max, true), Rand(pillarRoomSize.min, pillarRoomSize.max, true));
            (int x, int y) loc = (Rand(columns - size.w), Rand(rows - size.h));
            pillarRooms.Add((new(loc.x, loc.y), new(size.w, size.h)));

            for(int y = loc.y; y < loc.y + size.h; y++)
                for(int x = loc.x; x < loc.x + size.w; x++)
                    this[x, y] = Tile.Empty;

            if(Rand(100) >= staggeredPillarRoomChance) // staggered
            {
                bool inRoom(int x, int y) => x >= 0 && y >= 0 && x < size.w && y < size.h;

                int hStep = Rand(staggeredPillarStep.min, staggeredPillarStep.max, true), vStep = Rand(staggeredPillarStep.min, staggeredPillarStep.max, true);
                if(hStep == vStep && hStep == 1)
                    if(Rand(100) >= 50) hStep = 2;
                    else vStep = 2;

                for(int x = -size.h; x < size.w;)
                {
                    int nextX = x + 2*hStep;

                    for(int y = 0; y < size.h; y += vStep)
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
                int spacing = Rand(pillarSpacing.min, pillarSpacing.max, true);
                for(int x = 0; x < size.w; x += spacing)
                    for(int y = 0; y < size.h; y += spacing)
                        this[x + loc.x, y + loc.y] = Tile.Pillar;
            }
        }
    }

    public Tile[,] FormatTiles()
    {
        Tile[,] ret = new Tile[tiles.GetLength(1), tiles.GetLength(0)];

        for(int x = 0; x < ret.GetLength(0); x++)
            for(int y = 0; y < ret.GetLength(1); y++)
                ret[x, y] = this[x, y];

        return ret;
    }

    public IEnumerator GetEnumerator() => tiles.GetEnumerator();


    private int Rand(int max, bool incl = false) => rand.Next(max + (incl ? 1 : 0));
    private int Rand(int min, int max, bool incl = false) => rand.Next(min, max + (incl ? 1 : 0));
}