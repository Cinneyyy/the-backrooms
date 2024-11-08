using Backrooms.Extensions;

namespace Backrooms.Debugging;

#pragma warning disable CA1806 // Do not ignore method results
public static unsafe class Atlas
{
    public static void Draw()
    {
        const int space = 16;

        Vec2i size = new(Renderer.res.y - 2 * space);
        Vec2i loc = new((Renderer.res.x - Renderer.res.y) / 2 + space, space);

        Vec2i cam = Camera.pos.floor;
        Vec2i cell = cam - size/2;

        uint* scan = Renderer.pixelData + Renderer.stride * loc.y + loc.x;

        for(int y = 0; y < size.y; y++)
        {
            for(int x = 0; x < size.x; x++)
            {
                *(scan + x) = cell switch
                {
                    _ when cell == cam => 0xff0000ffu,
                    _ when Map.curr[cell] == Tile.Void => *(scan + x),
                    _ => MapColor(Map.curr[cell])
                };

                cell.x++;
            }

            scan += Renderer.stride;

            cell.x -= size.x;
            cell.y++;
        }
    }

    public static (nint surface, nint tex) DrawFullToSurface()
    {
        Vec2i size = Map.curr.size;

        nint tex = SDL_CreateTexture(Renderer.sdlRend, SDL_PIXELFORMAT_RGBA8888, (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, size.x, size.y);

        SDL_LockTexture(tex, nint.Zero, out nint pixels, out int stride);
        uint* scan = (uint*)pixels;
        stride /= 4;

        for(int y = 0; y < size.y; y++)
        {
            for(int x = 0; x < size.x; x++)
                *(scan + x) = MapColor(Map.curr[x, y]);

            scan += stride;
        }

        nint surface = SDL_CreateRGBSurfaceWithFormatFrom(pixels, size.x, size.y, 32, stride * 4, SDL_PIXELFORMAT_RGBA8888);
        SDL_UnlockTexture(tex);

        return (surface, tex);
    }


    private static uint MapColor(Tile tile)
        => (tile switch
        {
            Tile.Void => 0u,
            Tile.Air => 0x202020u,
            Tile.BigRoomAir => 0x181818u,
            Tile.PillarRoomAir => 0x101010u,
            Tile.Wall => 0xaa8f00u,
            Tile.Pillar => 0x998c4du,
            _ => 0u
        }).AddAlpha();
}