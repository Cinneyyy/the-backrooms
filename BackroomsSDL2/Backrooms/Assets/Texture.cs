using System;

namespace Backrooms.Assets;

#pragma warning disable CA1806 // Do not ignore method results
public unsafe class Texture : IDisposable
{
    public Texture(string path)
    {
        sdlTex = IMG_LoadTexture(Renderer.sdlRend, path);
        SDL_QueryTexture(sdlTex, out _, out _, out size.x, out size.y);
    }


    ~Texture()
        => Dispose();


    public readonly Vec2i size;
    public readonly nint sdlTex;


    public LockedTexture Lock()
        => new(this);

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        SDL_DestroyTexture(sdlTex);
    }
}