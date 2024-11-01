using System;
using System.Runtime.InteropServices;

namespace Backrooms.Assets;

#pragma warning disable CA1806 // Do not ignore method results
public unsafe class LockedTexture : IDisposable
{
    public LockedTexture(string path)
    {
        nint surface = IMG_Load(path);
        SDL_Surface sdlSurface = Marshal.PtrToStructure<SDL_Surface>(surface);
        SDL_PixelFormat format = Marshal.PtrToStructure<SDL_PixelFormat>(sdlSurface.format);

        if(format.format != SDL_PIXELFORMAT_RGBA8888)
        {
            nint convertedSurface = SDL_ConvertSurfaceFormat(surface, SDL_PIXELFORMAT_RGBA8888, sdlSurface.flags);
            sdlTex = SDL_CreateTextureFromSurface(Renderer.sdlRend, convertedSurface);
            SDL_FreeSurface(convertedSurface);
        }
        else
        {
            sdlTex = SDL_CreateTextureFromSurface(Renderer.sdlRend, surface);
        }

        SDL_FreeSurface(surface);

        SDL_QueryTexture(sdlTex, out _, out int access, out size.x, out size.y);
        SDL_LockTexture(sdlTex, nint.Zero, out nint pixels, out stride);
        this.pixels = (uint*)pixels;

        Console.WriteLine((SDL_TextureAccess)access);
    }

    public LockedTexture(Texture texture)
    {
        size = texture.size;
        sdlTex = SDL_CreateTexture(Renderer.sdlRend, SDL_PIXELFORMAT_RGBA8888, (int)SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, size.x, size.y);

        SDL_SetRenderTarget(Renderer.sdlRend, sdlTex);
        SDL_RenderCopy(Renderer.sdlRend, texture.sdlTex, nint.Zero, nint.Zero);

        SDL_LockTexture(sdlTex, nint.Zero, out nint pixels, out stride);
        this.pixels = (uint*)pixels;
    }


    ~LockedTexture()
        => Dispose();


    public readonly Vec2i size;
    public readonly uint* pixels;
    public readonly nint sdlTex;
    public readonly int stride;


    public uint this[int offset]
    {
        get => pixels[offset];
        set => pixels[offset] = value;
    }

    public uint this[int x, int y]
    {
        get => pixels[y * stride + x];
        set => pixels[y * stride + x] = value;
    }

    public uint this[Vec2i loc]
    {
        get => pixels[loc.y * stride + loc.x];
        set => pixels[loc.y * stride + loc.x] = value;
    }


    public void Dispose()
    {
        GC.SuppressFinalize(this);
        SDL_DestroyTexture(sdlTex);
    }
}