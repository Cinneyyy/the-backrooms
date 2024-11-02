using Backrooms.Assets;
using Backrooms.Extensions;
using Backrooms.Lighting;

namespace Backrooms;

public class Program
{
    private static void Main(string[] args)
    {
        Window.Init(new Vec2i(1920, 1080) / 4, "Backrooms Game");

        Map.current = new(new byte[,]
        {
            { 1, 1, 1, 1, 1, 1 },
            { 1, 0, 0, 0, 0, 1 },
            { 1, 0, 1, 0, 0, 1 },
            { 1, 0, 0, 0, 0, 1 },
            { 1, 0, 0, 0, 0, 1 },
            { 1, 0, 0, 0, 0, 1 },
            { 1, 1, 1, 1, 1, 1 },
        })
        {
            ceilTex = Resources.GetLockedTexture("ceiling"),
            floorTex = Resources.GetLockedTexture("floor"),
            lightTex = Resources.GetLockedTexture("light"),
            textures = new()
            {
                [Tile.Wall] = Resources.GetLockedTexture("wall"),
                [Tile.Pillar] = Resources.GetLockedTexture("pillar")
            }
        };

        Camera.pos = Map.current.center.asVec2f;

        Window.Run();
    }
}