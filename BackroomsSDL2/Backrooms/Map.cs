using System.Collections.Generic;
using Backrooms.Assets;
using Backrooms.Extensions;

namespace Backrooms;

public class Map(Tile[,] tiles)
{
    public Map(byte[,] tiles) : this(default(Tile[,]))
    {
        size = tiles.Size();
        center = size / 2;
        this.tiles = new Tile[size.x, size.y];
        graffitis = new int[size.x, size.y];
    }


    public readonly Vec2i size = new(tiles.SizeX(), tiles.SizeY());
    public readonly Vec2i center = new(tiles.SizeX() / 2, tiles.SizeY() / 2);
    public readonly Dictionary<Tile, LockedTexture> textures = [];
    public LockedTexture[] graffitiTextures = [];
    public LockedTexture floorTex, ceilTex, lightTex;
    public float floorTexScale = 1f, ceilTexScale = 1f;
    public float floorLuminance = .5f, ceilLuminance = .5f;

    private readonly Tile[,] tiles = tiles;
    private readonly int[,] graffitis = new int[tiles.SizeX(), tiles.SizeY()];


    public Tile this[int x, int y]
    {
        get => InBounds(x, y) ? tiles[x, y] : Tile.Air;
        set
        {
            if(InBounds(x, y))
                tiles[x, y] = value;
        }
    }

    public Tile this[Vec2i pt]
    {
        get => InBounds(pt) ? tiles[pt.x, pt.y] : Tile.Air;
        set
        {
            if(InBounds(pt))
                tiles[pt.x, pt.y] = value;
        }
    }


    public bool InBounds(float x, float y)
        => x >= 0f && y >= 0f && x < size.x && y < size.y;
    public bool InBounds(Vec2f pt)
        => InBounds(pt.x, pt.y);
    public bool InBounds(int x, int y)
        => x >= 0 && y >= 0 && x < size.x && y < size.y;
    public bool InBounds(Vec2i pt)
        => InBounds(pt.x, pt.y);

    public bool OutOfBounds(float x, float y)
        => x < 0f || y < 0f || x >= size.x || y >= size.y;
    public bool OutOfBounds(Vec2f pt)
        => OutOfBounds(pt.x, pt.y);
    public bool OutOfBounds(int x, int y)
        => x < 0 || y < 0 || x >= size.x || y >= size.y;
    public bool OutOfBounds(Vec2i pt)
        => OutOfBounds(pt.x, pt.y);

    public void GenerateGraffitis(int amount, int seed = 0)
    {
        RNG.SetSeed(seed);

        for(int i = 0; i < amount; i++)
            graffitis[RNG.Range(size.x), RNG.Range(size.y)] = RNG.Range(graffitiTextures.Length) + 1;
    }

    public Tile TileAt(int x, int y) => this[x, y];
    public Tile TileAt(Vec2i pt) => this[pt];

    public LockedTexture TexAt(int x, int y) => textures[this[x, y]];
    public LockedTexture TexAt(Vec2i pt) => textures[this[pt]];

    public bool Intersects(Vec2f pt, out Tile tile)
    {
        tile = this[pt.floor];
        return tile.isSolid();
    }
    public bool Intersects(Vec2f pt, float radius, out Tile tile)
    {
        Vec2i t = pt.floor;
        Vec2f dec = pt - t;
    }
}