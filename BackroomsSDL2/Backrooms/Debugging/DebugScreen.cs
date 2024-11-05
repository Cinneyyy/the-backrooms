using Backrooms.Assets;
using Backrooms.Extensions;

namespace Backrooms.Debugging;

public static class DebugScreen
{
    private static readonly Font font = Resources.LoadFont("cascadia_code", 10);
    private static readonly SDL_Color color = new() { r = 0xff, g = 0xff, b = 0xff, a = 0xff };


    public static string GetDisplayStr()
        => $"FPS: {Window.fps}\n" +
           $"Map size / center: {Map.curr.size} / {Map.curr.center}\n" +
           $"Pos / Tile: {Camera.pos} / {Camera.pos.floor}\n" +
           $"Facing: {Camera.angle.ToDeg():0}°\n";

    public static void Draw()
        => Renderer.DrawAndRenderText(GetDisplayStr(), font, color, Vec2i.zero);
}