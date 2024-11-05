using System;
using System.Threading.Tasks;
using Backrooms.Assets;
using Backrooms.Extensions;
using Backrooms.Light;
using static System.Runtime.InteropServices.JavaScript.JSType;

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


    public static float wallHeight = 1f;
    public static bool fastColorBlend = true;

    private static Vec2i res, center;


    public static float[] depthBuf { get; private set; }
    public static ushort[] heightBuf { get; private set; }
    private static uint* pixels => Renderer.pixelData;
    private static int stride => Renderer.stride;


    public static void PrepareDraw()
    {
        Array.Fill(depthBuf, 1f);
        Array.Fill(heightBuf, (ushort)0);
    }

    public static void DrawWalls()
        => Parallel.For(0, res.x, DrawColumn);

    public static void DrawFloorAndCeil()
    //=> Parallel.For(0, center.y, DrawRow);
    {
        for(int y = 0; y < center.y; y++)
            DrawRow(y);
    }



    private static void DrawColumn(int x)
    {
        Vec2f dir = Camera.forward + Camera.plane * (2f*x / (res.x-1) - 1f);
        Vec2i mPos = Camera.pos.floor;

        Vec2f deltaDist = new(
            dir.x == 0f ? float.MaxValue : MathF.Abs(1f / dir.x),
            dir.y == 0f ? float.MaxValue : MathF.Abs(1f / dir.y));

        Vec2f sideDist = new(
            deltaDist.x * (dir.x < 0f ? (Camera.pos.x - mPos.x) : (mPos.x + 1f - Camera.pos.x)),
            deltaDist.y * (dir.y < 0f ? (Camera.pos.y - mPos.y) : (mPos.y + 1f - Camera.pos.y)));

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

            else if(Map.curr.IsSolid(mPos))
                hit = true;
        }

        float dist = vert ? (sideDist.x - deltaDist.x) : (sideDist.y - deltaDist.y);
        float normDist = float.Clamp(dist / Camera.renderDist, 0f, 1f);
        Vec2f hitPos = Camera.pos + dir * dist;

        if(depthBuf[x] < normDist || dist < 0f)
            return;

        depthBuf[x] = normDist;

        float height = wallHeight * res.y / dist;
        int halfHeight = (height / 2f).Floor();
        int y0 = int.Clamp(center.y - halfHeight, 0, res.y-1),
            y1 = int.Clamp(center.y + halfHeight, 0, res.y-1);

        heightBuf[x] = (ushort)halfHeight;

        float brightness = (vert ? .75f : .5f) * Fog.GetDistFogNormalized(normDist);
        if(Lighting.enabled)
            brightness = Lighting.distribution.ComputeLighting(brightness, hitPos);

        float wallX = (vert ? (Camera.pos.y + dist * dir.y) : (Camera.pos.x + dist * dir.x)) % 1f;
        if(side is Side.West or Side.South)
            wallX = 1f - wallX;

        LockedTexture tex = Map.curr.TexAt(mPos);
        int texX = (wallX * (tex?.bounds.x ?? 0)).Floor();
        if(vert && dir.x > 0f || !vert && dir.y < 0f)
            texX = tex.size.x - texX - 1;
        float texStep = tex.size.y / height;
        float texPos = (y0 - center.y + halfHeight) * texStep;

        int graffiti = Map.curr.graffitis[mPos.x, mPos.y];
        bool hasGraffiti = graffiti != 0;
        LockedTexture gTex;
        int gTexX;
        float gTexStep, gTexPos;

        if(hasGraffiti)
        {
            gTex = Map.curr.graffitiTextures[graffiti-1];
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

    private static void DrawRow(int y)
    {
        float rowDist = wallHeight * center.y / y;

        if(float.IsInfinity(rowDist) || float.IsNaN(rowDist))
            return;

        Vec2f step = rowDist * 2 * Camera.plane / res.x;
        Vec2f floor = Camera.pos + rowDist * (Camera.forward - Camera.plane);

        uint* floorScan = pixels + stride * (center.y + y);
        uint* ceilScan = pixels + stride * (center.y - 1 - y);
        //for(int x = 0; x < res.x; x++)
        Parallel.For(0, res.x, x =>
        {
            if(heightBuf[x] > y)
            {
                floor += step;
                floorScan++;
                ceilScan++;
                //continue;
                return;
            }

            Vec2i tile = floor.floor;
            Vec2f texFrac = floor - tile;
            Vec2i floorTex = (Map.curr.floorTex.size * texFrac * Map.curr.floorTexScale).floor & Map.curr.floorTex.bounds;

            bool isLightTile = Lighting.distribution.IsLightTile(tile);
            LockedTexture ceilingTex = isLightTile ? Map.curr.lightTex : Map.curr.ceilTex;
            Vec2i ceilTex = new(
                int.Clamp((ceilingTex.size.x * texFrac.x * Map.curr.ceilTexScale).Floor(), 0, ceilingTex.bounds.x),
                int.Clamp((ceilingTex.size.y * texFrac.y * Map.curr.ceilTexScale).Floor(), 0, ceilingTex.bounds.y));

            floor += step;

            float tileDist = (Camera.pos - floor).length;
            float fog = Fog.GetDistFog(tileDist);
            if(Lighting.enabled)
                fog = Lighting.distribution.ComputeLighting(fog, floor);

            uint* floorCol = Map.curr.floorTex.pixels + floorTex.y * Map.curr.floorTex.stride + floorTex.x;
            *floorScan++ = floorCol->MultiplyColor(Map.curr.floorLuminance * fog);

            uint* ceilCol = ceilingTex.pixels + ceilTex.y * ceilingTex.stride + ceilTex.x;

            if(Lighting.enabled && isLightTile && ceilCol->R() == 0xff && ceilCol->G() == 0xff && ceilCol->B() == 0xff && tileDist < 10f && Map.curr.IsAir(floor.floor))
                *ceilScan++ = 0xffffffff;
            else
                *ceilScan++ = ceilCol->MultiplyColor(Map.curr.ceilLuminance * fog);
        });
    }


    internal static void Init()
    {
        res = Renderer.res;
        center = Renderer.center;

        depthBuf = new float[res.x];
        heightBuf = new ushort[res.x];
    }
}