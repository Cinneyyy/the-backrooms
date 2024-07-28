using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Backrooms;

public class Map(Tile[,] tiles) : IEnumerable<Vec2i>
{
    /// <summary>Array of UnsafeGraphic WITHOUT transparency (=> Format24bppRgb)</summary>
    public UnsafeGraphic[] textures = [];
    /// <summary>Array of UnsafeGraphic WITH transparency (=> Format32bppArgb)</summary>
    public UnsafeGraphic[] graffitiTextures = [];
    /// <summary>UnsafeGraphic WITHOUT transparency (=> Format24bppRgb)</summary>
    public UnsafeGraphic floorTex, ceilTex;
    public float floorTexScale = 1f, ceilTexScale = 1f;
    public float floorLuminance = .5f, ceilLuminance = .5f;
    public int[,] graffitis;

    private Tile[,] tiles = tiles;
    private Vec2i _size = new(tiles?.Length0() ?? 0, tiles?.Length1() ?? 0);


    public Vec2i size => _size;
    public string[] texturesStr
    {
        set => LoadTextures(value);
    }
    public string floorTexStr
    {
        set => floorTex = new(Resources.sprites[value], false);
    }
    public string ceilTexStr
    {
        set => ceilTex = new(Resources.sprites[value], false);
    }
    public string[] graffitiTexturesStr
    {
        set => graffitiTextures = value.Select(id => new UnsafeGraphic(Resources.sprites[id], true)).ToArray();
    }


    public Map(bool[,] tiles) : this(new Tile[tiles.Length0(), tiles.Length1()])
    {
        for(int x = 0; x < tiles.Length0(); x++)
            for(int y = 0; y < tiles.Length1(); y++)
                this.tiles[x, y] = tiles[x, y] ? Tile.Wall : Tile.Air;
    }

    public Map(byte[,] tiles) : this(new Tile[tiles.GetLength(0), tiles.GetLength(1)])
    {
        for(int x = 0; x < tiles.Length0(); x++)
            for(int y = 0; y < tiles.Length1(); y++)
                this.tiles[x, y] = (Tile)tiles[x, y];
    }


    public Tile this[int x, int y]
    {
        get => InBounds(x, y) ? tiles[x, y] : Tile.Air;
        set {
            if(InBounds(x, y))
                tiles[x, y] = value;
        }
    }

    public Tile this[Vec2i v]
    {
        get => this[v.x, v.y];
        set => this[v.x, v.y] = value;
    }


    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();


    public IEnumerator<Vec2i> GetEnumerator()
    {
        for(int x = 0; x < tiles.Length0(); x++)
            for(int y = 0; y < tiles.Length1(); y++)
                yield return new(x, y);
    }

    public void Add(params Tile[] row)
    {
        tiles ??= new Tile[_size.x = row.Length, 0];

        if(tiles.Length1() != 0 && tiles.Length0() != row.Length)
            throw new("Cannot add row to map, as it is of different size/length");

        Utils.ResizeArr2D(ref tiles, row.Length, tiles.Length + 1);
        for(int x = 0; x < row.Length; x++)
            tiles[x, tiles.Length1() - 1] = row[x];
        _size.y++;
    }
    public void Add(params byte[] row)
        => Add(row.Cast<Tile>().ToArray());

    public void LoadTextures(params string[] textureIds)
        => textures = textureIds.Select(id => id is null ? null : new UnsafeGraphic(Resources.sprites[id], false)).ToArray();

    public void GenerateGraffitis(int amount, int? seed = null)
    {
        graffitis = new int[tiles.Length0(), tiles.Length1()];

        if(seed is not null)
            RNG.SetSeed(seed.Value);

        for(int i = 0; i < amount; i++)
            graffitis[RNG.Range(graffitis.Length0()), RNG.Range(graffitis.Length1())] = RNG.Range(graffitiTextures.Length) + 1;
    }

    public UnsafeGraphic TextureAt(int x, int y)
        => textures[(int)this[x, y]];
    public UnsafeGraphic TextureAt(Vec2i pos)
        => textures[(int)this[pos]];

    public bool Intersects(Vec2f pt, out Tile tile)
    {
        Vec2i idx = pt.Floor();

        if(idx.x < 0 || idx.x >= _size.x || idx.y < 0 || idx.y >= _size.y)
        {
            tile = Tile.Air;
            return false;
        }

        tile = tiles[idx.x, idx.y];
        return IsCollidingTile(tile);
    }
    public bool Intersects(Vec2f pt, float radius, out Tile type)
    {
        Vec2i tile = pt.Floor();
        Vec2f sub = pt % 1f;

        Vec2i offset = new(sub.x switch {
            _ when sub.x < radius => -1,
            _ when 1f - sub.x < radius => 1,
            _ => 0
        }, sub.y switch {
            _ when sub.y < radius => -1,
            _ when 1f - sub.y < radius => 1,
            _ => 0
        });

        if(offset.x == 0 || offset.y == 0) // Not edging or edging on one side
        {
            type = this[tile + offset];
            return IsCollidingTile(type);
        }

        Tile collA = this[tile.x + offset.x, tile.y],
             collB = this[tile.x, offset.y + tile.y],
             collC = this[tile + offset];

        // sorry
        return IsCollidingTile(type = IsCollidingTile(collA) ? collA : IsCollidingTile(collB) ? collB : IsCollidingTile(collC) ? collC : Tile.Air);
    }

    public Vec2f ResolveIntersectionIfNecessery(Vec2f oldPt, Vec2f newPt, float radius, out bool didCollide)
    {
        if(!(didCollide = Intersects(newPt, radius, out _)))
            return newPt;

        Vec2f potPt;
        return !Intersects(potPt = new(newPt.x, oldPt.y), radius, out _) ||
               !Intersects(potPt = new(oldPt.x, newPt.y), radius, out _)
               ? potPt : oldPt;
    }

    public bool InBounds(Vec2f loc)
        => loc.x >= 0 && loc.y >= 0 && loc.x < size.x && loc.y < size.y;
    public bool InBounds(float x, float y)
        => x >= 0 && y >= 0 && x < size.x && y < size.y;

    public void SetTiles(Tile[,] tiles)
    {
        this.tiles = tiles;
        _size = new(tiles.Length0(), tiles.Length1());
    }

    public bool LineOfSight(Vec2f a, Vec2f b)
    {
        if(IsCollidingTile(this[a.Floor()]) || IsCollidingTile(this[b.Floor()]))
            return false;

        Vec2f dir = (b - a).normalized;

        const float step_size = .1f;
        Vec2f step = dir * step_size;

        Vec2f curr = a;
        float distTraveled = 0f, totalDist = (b - a).length;
        while(distTraveled < totalDist)
        {
            distTraveled += step_size;
            curr += step;
            Vec2i tile = curr.Floor();

            if(IsCollidingTile(this[tile]))
                return false;
        }

        return true;
    }

    public IEnumerable<Vec2i> GetNeighbors(Vec2i cell)
        => from delta in Vec2i.directions
           let neighbor = cell + delta
           where InBounds(neighbor)
           select neighbor;
    public IEnumerable<Vec2i> GetNeighbors(Vec2i cell, Predicate<Tile> predicate)
        => from delta in Vec2i.directions
           let neighbor = cell + delta
           where InBounds(neighbor)
           where predicate(this[neighbor])
           select neighbor;


    public static bool IsEmptyTile(Tile tile)
        => tile is Tile.Air or Tile.BigRoomAir or Tile.PillarRoomAir;
    public static bool IsCollidingTile(Tile tile)
        => !IsEmptyTile(tile);
}