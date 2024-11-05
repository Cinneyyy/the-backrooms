using System.Runtime.CompilerServices;
using Backrooms.Assets;
using Backrooms.Debugging;

namespace Backrooms;

#pragma warning disable CA1806 // Do not ignore method results
public static unsafe class Renderer
{
    private static nint rawTex, abstractTex;
    private static SDL_Rect outputRect;


    public static uint* pixelData { get; private set; }
    public static int stride { get; private set; }
    public static nint sdlRend { get; private set; }
    public static Vec2i center { get; private set; }
    public static float downscale { get; private set; }
    public static float upscale { get; private set; }
    public static Vec2i outputOffset { get; private set; }
    public static Vec2i screen { get; private set; }

    private static Vec2i _res;
    public static Vec2i res
    {
        get => _res;
        set => Resize(value);
    }


    public static void Draw()
    {
        #region Initialization
        SDL_LockTexture(rawTex, nint.Zero, out nint _pixelData, out int _stride);
        pixelData = (uint*)_pixelData;
        stride = _stride / 4;

        //// TODO: remove once ceiling/floor rendering has been ported over
        //Unsafe.InitBlock(pixelData, 0, (uint)(4 * stride * res.y)); // Clear rawTex

        SDL_SetRenderTarget(sdlRend, abstractTex); // Clear abstractTex
        SDL_SetRenderDrawColor(sdlRend, 0, 0, 0, 0);
        SDL_RenderClear(sdlRend);
        #endregion

        #region Rendering
        Raycaster.PrepareDraw();
        Raycaster.DrawWalls();
        Raycaster.DrawFloorAndCeil();

        //DebugScreen.Draw();

        //LockedTexture tex = Raycaster.map.textures[Tile.Wall];
        //for(int y = 0; y < tex.size.y; y++)
        //{
        //    uint* scan = pixelData + stride * y;
        //    uint* texScan = tex.pixels + tex.stride * y;

        //    for(int x = 0; x < tex.size.x; x++)
        //        *(scan+x) = *(texScan+x);
        //}
        #endregion

        SDL_SetRenderTarget(sdlRend, nint.Zero);

        SDL_UnlockTexture(rawTex);
        SDL_RenderCopy(sdlRend, rawTex, nint.Zero, ref outputRect);
        SDL_RenderCopy(sdlRend, abstractTex, nint.Zero, ref outputRect);

        SDL_RenderPresent(sdlRend);
    }

    public static nint DrawText(string text, Font font, SDL_Color color)
    {
        nint surface = TTF_RenderText_Solid(font.sdlFont, text, color);
        if(surface == nint.Zero)
            throw new($"Failed to draw text to surface: {TTF_GetError()}");

        nint tex = SDL_CreateTextureFromSurface(sdlRend, surface);

        SDL_FreeSurface(surface);

        return tex;
    }

    public static void DrawAndRenderText(string text, Font font, SDL_Color color, Vec2i loc)
    {
        nint tex = DrawText(text, font, color);
        RenderTex(tex, loc);
        SDL_DestroyTexture(tex);
    }

    public static void RenderTex(Texture tex, SDL_Rect rect)
        => RenderTex(tex.sdlTex, rect);
    public static void RenderTex(Texture tex, Vec2i loc, Vec2i size)
        => RenderTex(tex.sdlTex, loc, size);
    public static void RenderTex(Texture tex, Vec2i loc)
        => RenderTex(tex.sdlTex, loc);
    public static void RenderTex(nint tex, SDL_Rect rect)
    {
        SDL_SetRenderTarget(sdlRend, abstractTex);
        SDL_SetRenderDrawColor(sdlRend, 0, 0, 0, 0xff);
        SDL_RenderFillRect(sdlRend, ref rect);
        SDL_RenderCopy(sdlRend, tex, nint.Zero, ref rect);
    }
    public static void RenderTex(nint tex, Vec2i loc, Vec2i size)
    {
        SDL_Rect rect = new()
        {
            x = loc.x,
            y = loc.y,
            w = size.x,
            h = size.y
        };

        SDL_SetRenderTarget(sdlRend, abstractTex);
        SDL_SetRenderDrawColor(sdlRend, 0, 0, 0, 0xff);
        SDL_RenderFillRect(sdlRend, ref rect);
        SDL_RenderCopy(sdlRend, tex, nint.Zero, ref rect);
    }
    public static void RenderTex(nint tex, Vec2i loc)
    {
        SDL_Rect rect = new() { x = loc.x, y = loc.y };
        SDL_QueryTexture(tex, out _, out _, out rect.w, out rect.h);

        SDL_SetRenderTarget(sdlRend, abstractTex);
        SDL_SetRenderDrawColor(sdlRend, 0, 0, 0, 0xff);
        SDL_RenderFillRect(sdlRend, ref rect);
        SDL_RenderCopy(sdlRend, tex, nint.Zero, ref rect);
    }


    private static void Resize(Vec2i res)
    {
        _res = res;
        center = res/2;

        DestroyTex();
        SDL_RenderClear(sdlRend);

        float resRatio = (float)res.x / res.y;
        float screenRatio = (float)screen.x / screen.y;

        if(resRatio > screenRatio) // res is wider than screen
        {
            upscale = (float)screen.x / res.x;
            Vec2i size = (res.asVec2f * upscale).floor;
            int yOffset = (screen.y - size.y) / 2;
            outputRect = new() { x = 0, y = yOffset, w = size.x, h = size.y };
        }
        else // screen is wider than or as wide as res
        {
            upscale = (float)screen.y / res.y;
            Vec2i size = (res.asVec2f * upscale).floor;
            int xOffset = (screen.x - size.x) / 2;
            outputRect = new() { x = xOffset, y = 0, w = size.x, h = size.y };
        }

        downscale = 1f / upscale;
        outputOffset = new(outputRect.x, outputRect.y);

        rawTex = SDL_CreateTexture(sdlRend, SDL_PIXELFORMAT_RGBA8888, (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, res.x, res.y);
        abstractTex = SDL_CreateTexture(sdlRend, SDL_PIXELFORMAT_RGBA8888, (int)SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, res.x, res.y);

        SDL_SetTextureScaleMode(rawTex, SDL_ScaleMode.SDL_ScaleModeNearest);
        SDL_SetTextureScaleMode(abstractTex, SDL_ScaleMode.SDL_ScaleModeNearest);

        /*SDL_BlendMode blendMode = SDL_ComposeCustomBlendMode(
            SDL_BlendFactor.SDL_BLENDFACTOR_SRC_COLOR,
            SDL_BlendFactor.SDL_BLENDFACTOR_DST_COLOR,
            SDL_BlendOperation.SDL_BLENDOPERATION_ADD,
            SDL_BlendFactor.SDL_BLENDFACTOR_DST_ALPHA,
            SDL_BlendFactor.SDL_BLENDFACTOR_ONE_MINUS_DST_ALPHA,
            SDL_BlendOperation.SDL_BLENDOPERATION_MAXIMUM);*/
        SDL_SetTextureBlendMode(abstractTex, SDL_BlendMode.SDL_BLENDMODE_BLEND);
    }


    internal static void Init(nint sdlWind, Vec2i res, Vec2i screen)
    {
        sdlRend = SDL_CreateRenderer(sdlWind, -1, SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
        if(sdlRend == nint.Zero)
            throw new($"Failed to create SDL renderer: {SDL_GetError()}");

        Renderer.screen = screen;

        Resize(res);

        Raycaster.Init();
    }

    internal static void DestroyTex()
    {
        SDL_UnlockTexture(rawTex);
        SDL_DestroyTexture(rawTex);
        SDL_DestroyTexture(abstractTex);
    }
}