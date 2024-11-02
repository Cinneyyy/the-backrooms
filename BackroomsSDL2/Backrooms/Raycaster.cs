using System;
using System.Threading.Tasks;
using Backrooms.Assets;
using Backrooms.Extensions;
using Backrooms.Lighting;

namespace Backrooms;

#pragma warning disable CA2211 // Non-constant fields should not be visible
public static unsafe class Raycaster
{
    public enum Side : byte
    {
        North,
        East,
        South,
        West
    }


    private const float DEFAULT_MAX_FOG_DIST = 20f;

    public static Camera camera = new(90f.ToRad(), DEFAULT_MAX_FOG_DIST);
    public static Map map = new(new Tile[0, 0]);
    public static float wallHeight = 1f;
    public static bool fastColorBlend = true;
    public static FogSettings fog = new(DEFAULT_MAX_FOG_DIST * .925f);
    public static LightingSettings lighting = new(new GridLightDistribution(5), false);

    private static Vec2i res, center;


    private static uint* pixels => Renderer.pixelData;
    private static int stride => Renderer.stride;
    public static float[] depthBuf { get; private set; }
    public static ushort[] heightBuf { get; private set; }


    public static void PrepareDraw()
    {
        Array.Fill(depthBuf, 1f);
        Array.Fill(heightBuf, (ushort)0);
    }

    public static void DrawWalls()
        => Parallel.For(0, res.x, DrawColumn);



    private static void DrawColumn(int x)
    {
        Vec2f dir = camera.forward + camera.plane * (2f*x / (res.x-1) - 1f);
        Vec2i mPos = camera.pos.floor;

        Vec2f deltaDist = new(
            dir.x == 0f ? float.MaxValue : MathF.Abs(1f / dir.x),
            dir.y == 0f ? float.MaxValue : MathF.Abs(1f / dir.y));

        Vec2f sideDist = new(
            deltaDist.x * (dir.x < 0f ? (camera.pos.x - mPos.x) : (mPos.x + 1f - camera.pos.x)),
            deltaDist.y * (dir.y < 0f ? (camera.pos.y - mPos.y) : (mPos.y + 1f - camera.pos.y)));

        Vec2i step = new(MathF.Sign(dir.x), MathF.Sign(dir.y));

        bool hit = false, vert = false;
        int steps = 0;
        Side side = 0;
        const int max_steps = 256;

        while(!hit)
        {
            if(sideDist.x < sideDist.y)
            {
                sideDist.x += deltaDist.x;
                mPos.x += step.x;
                vert = true;
                side = dir.x < 0f ? Side.West : Side.East;
            }
            else
            {
                sideDist.y += deltaDist.y;
                mPos.y += step.y;
                vert = false;
                side = dir.y < 0f ? Side.North : Side.South;
            }

            if(steps++ >= max_steps) // this instead of !map.InBounds(...), because I want rendering outside of map to be cool
                return;

            else if(map.IsSolid(mPos))
                hit = true;
        }

        float dist = vert ? (sideDist.x - deltaDist.x) : (sideDist.y - deltaDist.y);
        float normDist = float.Clamp(dist / camera.maxFogDist, 0f, 1f);
        Vec2f hitPos = camera.pos + dir * dist;

        if(depthBuf[x] < normDist || dist < 0f)
            return;

        depthBuf[x] = normDist;

        float height = wallHeight * res.y / dist;
        int halfHeight = (height / 2f).Floor();
        int y0 = int.Clamp(center.y - halfHeight, 0, res.y-1),
            y1 = int.Clamp(center.y + halfHeight, 0, res.y-1);

        heightBuf[x] = (ushort)halfHeight;

        float brightness = (vert ? .75f : .5f) * fog.GetDistFogNormalized(normDist);
        if(lighting.enabled)
            brightness = lighting.distribution.ComputeLighting(brightness, hitPos);

        float wallX = (vert ? (camera.pos.y + dist * dir.y) : (camera.pos.x + dist * dir.x)) % 1f;
        if(side is Side.West or Side.South)
            wallX = 1f - wallX;

        LockedTexture tex = map.TexAt(mPos);
        int texX = (wallX * (tex?.bounds.x ?? 0)).Floor();
        if(vert && dir.x > 0f || !vert && dir.y < 0f)
            texX = tex.size.x - texX - 1;
        float texStep = tex.size.y / height;
        float texPos = (y0 - center.y + halfHeight) * texStep;

        int graffiti = map.graffitis[mPos.x, mPos.y];
        bool hasGraffiti = graffiti != 0;
        LockedTexture gTex;
        int gTexX;
        float gTexStep, gTexPos;

        if(hasGraffiti)
        {
            gTex = map.graffitiTextures[graffiti-1];
            gTexX = (wallX * gTex.bounds.x).Floor();
            gTexStep = gTex.size.y / height;
            gTexPos = (y0 - center.y + halfHeight) * gTexStep;
        }
        else
            (gTex, gTexX, gTexStep, gTexPos) = (null, 0, 0f, 0f);

        uint* scan = pixels + y0*stride + x;

        for(int y = y0; y <= y1; y++)
        {
            if(hasGraffiti)
            {
                uint* gTexScan = gTex.pixels + ((int)gTexPos & gTex.bounds.y) * gTex.stride + gTexX;
                gTexPos += gTexStep;
                uint gPixel = *gTexScan;
                uint gAlpha = gPixel & 0xff;

                if(gAlpha != 0)
                {

                    if(gAlpha == 0xff)
                        *scan = gTexScan->MultiplyColor(brightness);
                    else
                        *scan = fastColorBlend
                            ? ColorExtension.BlendColorsCrude(*scan, *gTexScan, gAlpha/255f)
                            : ColorExtension.BlendColors(*scan, *gTexScan, gAlpha/255f);

                    scan += stride;
                    continue;
                }
            }

            uint* texScan = tex.pixels + ((int)texPos & tex.bounds.y) * tex.stride + texX;
            *scan = texScan->MultiplyColor(brightness);
            //*scan = ColorExtension.JoinColor(255f * x / res.x, 255f * y / res.y, 0f, 0xff);

            texPos += texStep;
            scan += stride;
        }
    }


    internal static void Init()
    {
        res = Renderer.res;
        center = Renderer.center;

        depthBuf = new float[res.x];
        heightBuf = new ushort[res.x];
    }
}