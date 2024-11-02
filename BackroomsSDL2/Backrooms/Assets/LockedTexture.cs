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
        SDL_PixelFormat sdlFormat = Marshal.PtrToStructure<SDL_PixelFormat>(sdlSurface.format);
        (size.x, size.y) = (sdlSurface.w, sdlSurface.h);
        bounds = size - Vec2i.one;

        sdlTex = SDL_CreateTexture(Renderer.sdlRend, SDL_PIXELFORMAT_RGBA8888, (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, size.x, size.y);

        if(SDL_LockTexture(sdlTex, nint.Zero, out nint pixels, out stride) < 0)
            throw new($"Failed to lock texture: {SDL_GetError()}");
        this.pixels = (uint*)pixels;
        stride /= 4;

        if(SDL_ConvertPixels(size.x, size.y, sdlFormat.format, sdlSurface.pixels, sdlSurface.pitch, SDL_PIXELFORMAT_RGBA8888, pixels, stride * 4) < 0)
            throw new(SDL_GetError());

        SDL_FreeSurface(surface);
    }

    public LockedTexture(Texture texture)
    {
        size = texture.size;
        bounds = texture.bounds;
        sdlTex = SDL_CreateTexture(Renderer.sdlRend, SDL_PIXELFORMAT_RGBA8888, (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, size.x, size.y);
        if(SDL_LockTexture(sdlTex, nint.Zero, out nint pixels, out stride) < 0)
            throw new($"Failed to lock texture: {SDL_GetError()}");
        this.pixels = (uint*)pixels;
        stride /= 4;

        nint targetTex = SDL_CreateTexture(Renderer.sdlRend, SDL_PIXELFORMAT_RGBA8888, (int)SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, size.x, size.y);

        SDL_SetRenderTarget(Renderer.sdlRend, targetTex);
        SDL_RenderCopy(Renderer.sdlRend, texture.sdlTex, nint.Zero, nint.Zero);

        SDL_Rect rect = new() { x = 0, y = 0, w = size.x, h = size.y };
        SDL_RenderReadPixels(Renderer.sdlRend, ref rect, SDL_PIXELFORMAT_RGBA8888, pixels, stride);

        SDL_DestroyTexture(targetTex);
    }


    ~LockedTexture()
        => Dispose();


    public readonly Vec2i size;
    public readonly Vec2i bounds;
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
        SDL_UnlockTexture(sdlTex);
        SDL_DestroyTexture(sdlTex);
    }

    public Texture Unlock(bool dispose)
    {
        Texture tex = new(this);

        if(dispose)
            Dispose();

        return tex;
    }
}