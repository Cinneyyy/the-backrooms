using Backrooms.Assets;

namespace Backrooms;

#pragma warning disable CA1806 // Do not ignore method results
public static unsafe class Renderer
{
    private static nint sdlTex;
    private static Vec2i screen;
    private static SDL_Rect outputRect;


    public static uint* pixelData { get; private set; }
    public static int stride { get; private set; }
    public static nint sdlRend { get; private set; }

    private static Vec2i _res;
    public static Vec2i res
    {
        get => _res;
        set
        {
            _res = value;

            DestroyTex();

            Init(sdlRend, value, screen);
        }
    }


    public static void Draw()
    {
        SDL_SetRenderTarget(sdlRend, nint.Zero);
        SDL_RenderCopy(sdlRend, sdlTex, nint.Zero, ref outputRect);
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

    public static void RenderTex(nint tex, SDL_Rect rect)
    {
        SDL_SetRenderTarget(sdlRend, sdlTex);
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

        SDL_SetRenderTarget(sdlRend, sdlTex);
        SDL_RenderCopy(sdlRend, tex, nint.Zero, ref rect);
    }
    public static void RenderTex(nint tex, Vec2i loc)
    {
        SDL_Rect rect = new() { x = loc.x, y = loc.y };
        SDL_QueryTexture(tex, out _, out _, out rect.w, out rect.h);

        SDL_SetRenderTarget(sdlRend, sdlTex);
        SDL_RenderCopy(sdlRend, tex, nint.Zero, ref rect);
    }


    internal static void Init(nint sdlRend, Vec2i res, Vec2i screen)
    {
        _res = res;
        Renderer.screen = screen;
        Renderer.sdlRend = sdlRend;

        float resRatio = (float)res.x / res.y;
        float screenRatio = (float)screen.x / screen.y;

        if(resRatio > screenRatio) // res is wider than screen
        {
            Vec2i size = (res.asVec2f * ((float)screen.x / res.x)).floor;
            int yOffset = (screen.y - size.y) / 2;
            outputRect = new() { x = 0, y = yOffset, w = size.x, h = size.y };
        }
        else // screen is wider than or as wide as res
        {
            Vec2i size = (res.asVec2f * ((float)screen.y / res.y)).floor;
            int xOffset = (screen.x - size.x) / 2;
            outputRect = new() { x = xOffset, y = 0, w = size.x, h = size.y };
        }

        sdlTex = SDL_CreateTexture(sdlRend, SDL_PIXELFORMAT_RGBA8888, (int)SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, res.x, res.y);

        SDL_SetTextureScaleMode(sdlTex, SDL_ScaleMode.SDL_ScaleModeNearest);
        SDL_SetTextureBlendMode(sdlTex, SDL_BlendMode.SDL_BLENDMODE_NONE);

        SDL_LockTexture(sdlTex, nint.Zero, out nint pixelData, out int stride);
        Renderer.pixelData = (uint*)pixelData;
        Renderer.stride = stride;

        Raycaster.Init(res);
    }

    internal static void DestroyTex()
    {
        SDL_UnlockTexture(sdlTex);
        SDL_DestroyTexture(sdlTex);
    }
}