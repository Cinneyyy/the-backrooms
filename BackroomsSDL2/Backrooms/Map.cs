using System;
using System.Collections.Generic;
using System.Linq;
using Backrooms.Assets;
using Backrooms.Extensions;

namespace Backrooms;

#pragma warning disable CA2211 // Non-constant fields should not be visible
public class Map
{
    public Map(byte[,] tiles, Vec2i? spawnLocation)
    {
        size = tiles.Size();
        center = size / 2;
        graffitis = new int[size.x, size.y];
        textures = Enum.GetValues<Tile>().ToDictionary(t => t, t => default(LockedTexture));

        this.tiles = new Tile[size.x, size.y];
        for(int x = 0; x < size.x; x++)
            for(int y = 0; y < size.y; y++)
                this.tiles[x, y] = (Tile)tiles[x, y];

        if(!spawnLocation.HasValue)
        {
            int offset = 0;
            do
            {
                this.spawnLocation = new(center.x + offset, center.y);
                offset++;
            }
            while(this[this.spawnLocation].IsSolid());
        }
        else
            this.spawnLocation = spawnLocation.Value;
        spawnLocationF = this.spawnLocation + Vec2f.half;
    }

    public Map(Tile[,] tiles, Vec2i? spawnLocation)
    {
        this.tiles = tiles;
        size = tiles.Size();
        center = size / 2;
        graffitis = new int[size.x, size.y];
        textures = Enum.GetValues<Tile>().ToDictionary(t => t, t => default(LockedTexture));

        if(!spawnLocation.HasValue)
        {
            int offset = 0;
            do
            {
                this.spawnLocation = new(center.x + offset, center.y);
                offset++;
            }
            while(this[this.spawnLocation].IsSolid());
        }
        else
            this.spawnLocation = spawnLocation.Value;
        spawnLocationF = this.spawnLocation + Vec2f.half;
    }


    public LockedTexture[] graffitiTextures = [];
    public LockedTexture floorTex, ceilTex, lightTex;
    public float floorTexScale = .1f, ceilTexScale = 1f;
    public float floorLuminance = .5f, ceilLuminance = .5f;

    public static Map curr;


    public int[,] graffitis { get; private set; }
    public Vec2i size { get; private set; }
    public Vec2i center { get; private set; }
    public Vec2i spawnLocation { get; private set; }
    public Vec2f spawnLocationF { get; private set; }
    public Tile[,] tiles { get; private set; }
    public Dictionary<Tile, LockedTexture> textures { get; init; }


    public Tile this[int x, int y]
    {
        get => InBounds(x, y) ? tiles[x, y] : Tile.Void;
        set
        {
            if(InBounds(x, y))
                tiles[x, y] = value;
        }
    }

    public Tile this[Vec2i cell]
    {
        get => InBounds(cell) ? tiles[cell.x, cell.y] : Tile.Void;
        set
        {
            if(InBounds(cell))
                tiles[cell.x, cell.y] = value;
        }
    }


    public void SetTiles(Tile[,] tiles, Vec2i? spawnLocation)
    {
        this.tiles = tiles;
        size = tiles.Size();
        center = size / 2;
        graffitis = new int[size.x, size.y];

        if(!spawnLocation.HasValue)
        {
            int offset = 0;
            do
            {
                this.spawnLocation = new(center.x + offset, center.y);
                offset++;
            }
            while(this[this.spawnLocation].IsSolid());
        }
        else
            this.spawnLocation = spawnLocation.Value;
        spawnLocationF = this.spawnLocation + Vec2f.half;
    }

    public bool InBounds(float x, float y)
        => x >= 0f && y >= 0f && x < size.x && y < size.y;
    public bool InBounds(Vec2f pt)
        => InBounds(pt.x, pt.y);
    public bool InBounds(int x, int y)
        => x >= 0 && y >= 0 && x < size.x && y < size.y;
    public bool InBounds(Vec2i cell)
        => InBounds(cell.x, cell.y);

    public bool OutOfBounds(float x, float y)
        => x < 0f || y < 0f || x >= size.x || y >= size.y;
    public bool OutOfBounds(Vec2f pt)
        => OutOfBounds(pt.x, pt.y);
    public bool OutOfBounds(int x, int y)
        => x < 0 || y < 0 || x >= size.x || y >= size.y;
    public bool OutOfBounds(Vec2i cell)
        => OutOfBounds(cell.x, cell.y);

    public void GenerateGraffitis(int amount, int seed = 0)
    {
        RNG.SetSeed(seed);

        for(int i = 0; i < amount; i++)
            graffitis[RNG.Range(size.x), RNG.Range(size.y)] = RNG.Range(graffitiTextures.Length) + 1;
    }

    public Tile TileAt(int x, int y) => this[x, y];
    public Tile TileAt(Vec2i cell) => this[cell];

    public LockedTexture TexAt(int x, int y) => textures[this[x, y]];
    public LockedTexture TexAt(Vec2i cell) => textures[this[cell]];

    public bool Intersects(Vec2f pt, out Tile tile)
    {
        tile = this[pt.floor];
        return tile.IsSolid();
    }
    public bool Intersects(Vec2f pt, float radius, out Tile tile)
    {
        Vec2i t = pt.floor;
        Vec2f dec = pt - t;

        Vec2i offset = new(dec.x switch
        {
            _ when dec.x < radius => -1,
            _ when 1f - dec.x < radius => 1,
            _ => 0
        }, dec.y switch
        {
            _ when dec.y < radius => -1,
            _ when 1f - dec.y < radius => 1,
            _ => 0
        });

        if(offset == Vec2i.zero)
        {
            tile = this[t + offset];
            return tile.IsSolid();
        }

        Tile collA = this[t.x + offset.x, t.y],
             collB = this[t.x, t.y + offset.y],
             collC = this[t + offset];

        tile =
            collA.IsSolid() ? collA
            : collB.IsSolid() ? collB
            : collC.IsSolid() ? collB
            : this[t];
        return tile.IsSolid();
    }

    public Vec2f ResolveIntersectionIfNecessery(Vec2f oldPt, Vec2f newPt, float radius, out bool intersecting)
    {
        intersecting = Intersects(newPt, radius, out _);
        if(!intersecting)
            return newPt;

        // Only move along y-axis
        Vec2f pt = new(oldPt.x, newPt.y);
        if(!Intersects(pt, radius, out _))
            return pt;

        // Only move along x-axis
        pt = new(newPt.x, oldPt.y);
        if(!Intersects(pt, radius, out _))
            return pt;

        // Use old point
        return oldPt;
    }

    public bool LineOfSight(Vec2f a, Vec2f b)
    {
        if(this[a.floor].IsSolid() || this[b.floor].IsSolid())
            return false;

        const float step_size = .1f;
        Vec2f step = (b - a).WithLength(step_size);

        Vec2f curr = a;
        float distTraveled = 0f, totalDist = (b - a).length;
        while(distTraveled < totalDist)
        {
            distTraveled += step_size;
            curr += step;
            Vec2i tile = curr.floor;

            if(this[tile].IsSolid())
                return false;
        }

        return true;
    }

    public IEnumerable<Vec2i> GetNeighbors(Vec2i cell)
        => Vec2i.directions
        .Select(d => d + cell)
        .Where(InBounds);
    public IEnumerable<Vec2i> GetNeighbors(Vec2i cell, Predicate<Tile> predicate)
        => Vec2i.directions
        .Select(d => d + cell)
        .Where(n => predicate(this[n]))
        .Where(InBounds);

    public bool IsSolid(int x, int y)
        => this[x, y].IsSolid();
    public bool IsSolid(Vec2i cell)
        => this[cell].IsSolid();

    public bool isAir(int x, int y)
        => this[x, y].IsAir();
    public bool IsAir(Vec2i cell)
        => this[cell].IsAir();
}