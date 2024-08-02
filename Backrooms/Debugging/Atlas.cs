using Backrooms.PostProcessing;

namespace Backrooms.Debugging;

public class Atlas(Map map, Camera cam, Vec2i size, Vec2i loc) : PostProcessEffect()
{
    public readonly Map map = map;
    public readonly Camera cam = cam;
    public Vec2i size = size, loc = loc;


    public override bool requireRefBitmap => false;


    protected override unsafe void Exec(byte* scan0, int stride, int w, int h)
    {
        scan0 += loc.y*stride + loc.x*3;
        Vec2i cam = this.cam.pos.Floor(), tile = cam - size/2;

        for(int y = 0; y < size.y; y++)
        {
            for(int x = 0; x < size.x; x++)
            {
                Color32 col = tile switch {
                    _ when tile == cam => new(0xff0000),
                    _ when !map.InBounds(tile) => new(0),
                    _ => map[tile] switch {
                        Tile.Air => new(0x20),
                        Tile.BigRoomAir => new(0x18),
                        Tile.PillarRoomAir => new(0x10),
                        Tile.Wall => new(0xaa, 0x8f, 0),
                        Tile.Pillar => new(0x99, 0x8c, 0x4d),
                        _ => Color32.black
                    }
                };

                *(scan0+3*x) = col.b;
                *(scan0+3*x+1) = col.g;
                *(scan0+3*x+2) = col.r;

                tile.x++;
            }

            scan0 += stride;

            tile.x -= size.x;
            tile.y++;
        }
    }
    protected override unsafe void Exec(byte* scan0, byte* refScan0, int stride, int w, int h) => ThrowWrongExecExc<Atlas>();
}