using System;

namespace Backrooms.Assets;

#pragma warning disable CA1806 // Do not ignore method results
public unsafe class Texture : IDisposable
{
    public Texture(string path)
    {
        sdlTex = IMG_LoadTexture(Renderer.sdlRend, path);
        SDL_QueryTexture(sdlTex, out _, out _, out size.x, out size.y);
        bounds = size - Vec2i.one;
    }

    public Texture(LockedTexture tex)
    {
        size = tex.size;
        bounds = tex.bounds;

        nint surface = SDL_CreateRGBSurfaceWithFormatFrom((nint)tex.pixels, size.x, size.y, 32, tex.stride, SDL_PIXELFORMAT_RGBA8888);

        sdlTex = SDL_CreateTextureFromSurface(Renderer.sdlRend, surface);

        SDL_FreeSurface(surface);
    }


    ~Texture()
        => Dispose();


    public readonly Vec2i size;
    public readonly Vec2i bounds;
    public readonly nint sdlTex;


    public LockedTexture Lock(bool dispose)
    {
        LockedTexture tex = new(this);

        if(dispose)
            Dispose();

        return tex;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        SDL_DestroyTexture(sdlTex);
    }
}