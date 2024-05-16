using System.Collections;
using System.Linq;

namespace Backrooms;

public class Map(Tile[,] tiles) : IEnumerable
{
    public LockedBitmap[] textures = [];

    private Tile[,] tiles = tiles;
    private Vec2i _size = new(tiles?.GetLength(0) ?? 0, tiles?.GetLength(1) ?? 0);


    public Vec2i size => _size;


    public Map(bool[,] tiles) : this(new Tile[tiles.GetLength(0), tiles.GetLength(1)])
    {
        for(int x = 0; x < tiles.GetLength(0); x++)
            for(int y = 0; y < tiles.GetLength(0); y++)
                this.tiles[x, y] = tiles[x, y] ? Tile.Wall : Tile.Empty;
    }

    public Map(byte[,] tiles) : this(new Tile[tiles.GetLength(0), tiles.GetLength(1)])
    {
        for(int x = 0; x < tiles.GetLength(0); x++)
            for(int y = 0; y < tiles.GetLength(0); y++)
                this.tiles[x, y] = (Tile)tiles[x, y];
    }


    public Tile this[int x, int y]
    {
        get => InBounds(x, y) ? tiles[x, y] : Tile.Empty;
        set => tiles[x, y] = value;
    }

    public Tile this[Vec2i v]
    {
        get => this[v.x, v.y];
        set => this[v.x, v.y] = value;
    }


    IEnumerator IEnumerable.GetEnumerator() => tiles.GetEnumerator();


    public void Add(params Tile[] row)
    {
        tiles ??= new Tile[_size.x = row.Length, 0];

        if(tiles.GetLength(1) != 0 && tiles.GetLength(0) != row.Length)
            throw new("Cannot add row to map, as it is of different size/length");

        Utils.ResizeArr2D(ref tiles, row.Length, tiles.Length + 1);
        for(int x = 0; x < row.Length; x++)
            tiles[x, tiles.GetLength(1)-1] = row[x];
        _size.y++;
    }
    public void Add(params byte[] row)
        => Add(row.Cast<Tile>().ToArray());

    public bool Intersects(Vec2f pt, out Tile type)
    {
        Vec2i idx = Round(pt);

        if(idx.x < 0 || idx.x >= _size.x || idx.y < 0 || idx.y >= _size.y)
        {
            type = Tile.Empty;
            return false;
        } 

        type = tiles[idx.x, idx.y];
        return type != Tile.Empty;
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
        return IsCollidingTile(type = collA != Tile.Empty ? collA : collB != Tile.Empty ? collB : collC != Tile.Empty ? collC : Tile.Empty);
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
        _size = new(tiles.GetLength(0), tiles.GetLength(1));
    }


    public static Vec2i Round(Vec2f pt)
        => pt.Round();

    public static bool IsCollidingTile(Tile tile)
        => tile != Tile.Empty;
}