using Backrooms.Assets;
using Backrooms.Extensions;
using Backrooms.Lighting;

namespace Backrooms;

public class Program
{
    private static void Main(string[] args)
    {
        Window.Init(new Vec2i(1920, 1080) / 4, "Backrooms Game");

        Camera cam;
        Scene.current = new()
        {
            map = new(new byte[,]
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
            },
            cam = cam = new(90f.ToRad(), 0f),
            camController = new(cam),
            fog = new(0f),
            lighting = new(new GridLightDistribution(5), false)
        };

        cam.pos = Raycaster.map.center.asVec2f;
        cam.renderDist = 20f;
        Scene.current.fog.maxDist = 20f * .925f;

        Window.Run();
    }
}