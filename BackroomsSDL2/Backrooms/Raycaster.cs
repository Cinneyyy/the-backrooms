using System;
using System.Drawing;
using System.Threading.Tasks;

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

    public static Camera camera = new(90f, DEFAULT_MAX_FOG_DIST);
    public static float maxFogDist;


    private static Vec2i res => Renderer.res;

    public static float[] depthBuf { get; private set; }
    public static ushort[] heightBuf { get; private set; }
    public static float fogCoefficient { get; private set; }

    private static float _fogEpsilon = 0.015625f; // 2^-6
    public static float fogEpsilon
    {
        get => _fogEpsilon;
        set
        {
            _fogEpsilon = value;
            fogCoefficient = MathF.Log(value) / fogMaxDist;
        }
    }

    private static float _fogMaxDist = DEFAULT_MAX_FOG_DIST - 1f;
    public static float fogMaxDist
    {
        get => _fogMaxDist;
        set
        {
            _fogMaxDist = value;
            fogCoefficient = MathF.Log(fogEpsilon) / value;
        }
    }


    public static void PrepareDraw()
    {
        Array.Fill(depthBuf, 1f);
        Array.Fill(heightBuf, (ushort)0);
    }

    //public static void DrawWalls()
    //    => Parallel.For(0, res.x, DrawColumn);

#if false
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

            else if(Map.IsCollidingTile(map[mPos]))
                hit = true;
        }

        float dist = vert ? (sideDist.x - deltaDist.x) : (sideDist.y - deltaDist.y);
        float normDist = Utils.Clamp01(dist / camera.maxFogDist);
        Vec2f hitPos = camera.pos + dir * dist;

        if(depthBuf[x] < normDist || dist < 0f)
            return;

        depthBuf[x] = normDist;

        float height = wallHeight * virtRes.y / dist;
        int halfHeight = (height / 2f).Floor();
        int y0 = Utils.Clamp(virtCenter.y - halfHeight, 0, virtRes.y-1),
            y1 = Utils.Clamp(virtCenter.y + halfHeight, 0, virtRes.y-1);

        heightBuf[x] = (ushort)halfHeight;

        float brightness = (vert ? .75f : .5f) * GetDistanceFog(dist);
        if(lightingEnabled)
            brightness = lightDistribution.ComputeLighting(this, brightness, hitPos);

        float wallX = (vert ? (camera.pos.y + dist * dir.y) : (camera.pos.x + dist * dir.x)) % 1f;
        if(side is Side.West or Side.South)
            wallX = 1f - wallX;

        UnsafeGraphic tex = map.TextureAt(mPos);
        int texX = (wallX * (tex?.wb ?? 0)).Floor();
        if(vert && dir.x > 0f || !vert && dir.y < 0f)
            texX = tex.w - texX - 1;
        float texStep = tex.h / height;
        float texPos = (y0 - virtCenter.y + halfHeight) * texStep;

        int graffiti = map.graffitis[mPos.x, mPos.y];
        bool hasGraffiti = graffiti != 0;
        UnsafeGraphic gTex;
        int gTexX;
        float gTexStep, gTexPos;

        if(hasGraffiti)
        {
            gTex = map.graffitiTextures[graffiti-1];
            gTexX = (wallX * gTex.wb).Floor();
            gTexStep = gTex.h / height;
            gTexPos = (y0 - virtCenter.y + halfHeight) * gTexStep;
        }
        else
            (gTex, gTexX, gTexStep, gTexPos) = (null, 0, 0f, 0f);

        byte* scan = (byte*)data.Scan0 + y0*data.Stride + x*3;

        for(int y = y0; y <= y1; y++)
        {
            if(hasGraffiti)
            {
                byte* gTexScan = gTex.scan0 + ((int)gTexPos & gTex.hb)*gTex.stride + gTexX*4;
                gTexPos += gTexStep;
                byte alpha = *(gTexScan+3);

                if(alpha == 0xff)
                {
                    *scan = (byte)(*gTexScan * brightness);
                    *(scan+1) = (byte)(*(gTexScan+1) * brightness);
                    *(scan+2) = (byte)(*(gTexScan+2) * brightness);
                }
                else if(alpha != 0)
                {
                    if(useFastColorBlend)
                        (*(scan+2), *(scan+1), *scan) =
                            Utils.BlendColorsCrude(
                                *(scan+2), *(scan+1), *scan,
                                (byte)(*(gTexScan+2) * brightness), (byte)(*(gTexScan+1) * brightness), (byte)(*gTexScan * brightness),
                                alpha/255f);
                    else
                        (*(scan+2), *(scan+1), *scan) =
                            Utils.BlendColors(
                                *(scan+2), *(scan+1), *scan,
                                (byte)(*(gTexScan+2) * brightness), (byte)(*(gTexScan+1) * brightness), (byte)(*gTexScan * brightness),
                        alpha/255f);
                }

                if(alpha != 0)
                {
                    scan += data.Stride;
                    continue;
                }
            }

            byte* texScan = tex.scan0 + ((int)texPos & tex.hb)*tex.stride + texX*3;
            texPos += texStep;

            *scan = (byte)(*texScan * brightness);
            *(scan+1) = (byte)(*(texScan+1) * brightness);
            *(scan+2) = (byte)(*(texScan+2) * brightness);

            scan += data.Stride;
        }
    }
#endif

    internal static void Init(Vec2i res)
    {
        depthBuf = new float[res.x];
        heightBuf = new ushort[res.x];
    }
}