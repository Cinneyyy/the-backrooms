using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using Backrooms.Gui;
using Backrooms.PostProcessing;

namespace Backrooms;

public unsafe class Renderer
{
    public Camera camera;
    public Input input;
    public Map map;
    public Window window;
    public readonly List<SpriteRenderer> sprites = [];
    public readonly List<PostProcessEffect> postProcessEffects = [];
    public readonly List<GuiGroup> guiGroups = [];
    public bool drawIfCursorOffscreen = true;
    public float[] depthBuf;
    public ushort[] heightBuf;
    public event Action dimensionsChanged;
    public float wallHeight = 1f;
    public bool useFastColorBlend = true;
    public bool lighting = true;
    public float lightSpacing = 10f, lightStrength = 2.25f;

    private float _fogEpsilon, _fogMaxDist;
    private readonly Comparison<SpriteRenderer> sprComparison;
    private readonly ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };


    public Vec2i virtRes { get; private set; }
    public Vec2i physRes { get; private set; }
    public Vec2i virtCenter { get; private set; }
    public Vec2i physCenter { get; private set; }
    public Vec2i outputRes { get; private set; }
    public Vec2i outputLocation { get; private set; }
    public Vec2f downscaleFactor { get; private set; }
    public Vec2f upscaleFactor { get; private set; }
    public float singleDownscaleFactor { get; private set; }
    public float singleUpscaleFactor { get; private set; }
    public float virtRatio { get; private set; }
    public float physRatio { get; private set; }
    public float fogCoefficient { get; private set; }
    public float fogEpsilon
    {
        get => _fogEpsilon;
        set {
            _fogEpsilon = value;
            fogCoefficient = MathF.Log(value) / fogMaxDist;
        }
    }
    public float fogMaxDist
    {
        get => _fogMaxDist;
        set {
            _fogMaxDist = value;
            fogCoefficient = MathF.Log(fogEpsilon) / value;
        }
    }


    public Renderer(Vec2i virtRes, Vec2i physRes, Window window)
    {
        this.window = window;
        UpdateResolution(virtRes, physRes);

        camera = new(90f, 20f, 0f);
        _fogEpsilon = 0.015625f;
        fogMaxDist = camera.maxRenderDist - 1f;

        sprComparison = (a, b) => {
            float aDist = (camera.pos - a.pos).sqrLength, bDist = (camera.pos - b.pos).sqrLength;

            if(bDist.CompareTo(aDist) is int comparison && comparison != 0)
                return comparison;
            else
                return b.importance.CompareTo(a.importance);
        };
    }


    public GuiGroup FindGuiGroup(string name)
        => guiGroups.Find(g => g.name == name);

    public void UpdateResolution(Vec2i virtRes, Vec2i physRes)
    {
        this.virtRes = virtRes;
        this.physRes = physRes;

        virtCenter = virtRes/2;
        physCenter = physRes/2;

        downscaleFactor = (Vec2f)virtRes / physRes;
        upscaleFactor = 1f / downscaleFactor;

        singleDownscaleFactor = downscaleFactor.min;
        singleUpscaleFactor = 1f / singleDownscaleFactor;

        virtRatio = (float)virtRes.x / virtRes.y;
        physRatio = (float)physRes.x / physRes.y;

        if(physRatio > virtRatio)
            outputRes = new((int)(physRes.y * virtRatio), physRes.y);
        else
            outputRes = new(physRes.x, (int)(physRes.x / virtRatio));

        outputLocation = (physRes - outputRes) / 2;
        depthBuf = new float[virtRes.x];
        heightBuf = new ushort[virtRes.x];

        dimensionsChanged?.Invoke();
    }

    public bool PrepareDraw(out Bitmap bitmap, out BitmapData data)
    {
        if(camera is null || map is null || !drawIfCursorOffscreen && input.cursorOffScreen)
        {
            bitmap = null;
            data = null;
            return false;
        }

        Array.Fill(depthBuf, 1f);
        Array.Fill(heightBuf, (ushort)0u);

        bitmap = new(virtRes.x, virtRes.y);
        data = bitmap.LockBits(new(0, 0, virtRes.x, virtRes.y), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

        return true;
    }

    public void DrawWalls(BitmapData data)
        => Parallel.For(0, data.Width, parallelOptions, x => DrawColumn(data, x));

    public void DrawWallsNonParallel(BitmapData data)
    {
        for(int x = 0; x < data.Width; x++)
            DrawColumn(data, x);
    }

    public void DrawFloorAndCeil(BitmapData data)
        => Parallel.For(0, data.Height/2, y => DrawRow(data, y));

    public void DrawFloorAndCeilNonParallel(BitmapData data)
    {
        for(int y = 0, c = data.Height/2; y < c; y++)
            DrawRow(data, y);
    }

    public void DrawSprites(BitmapData data)
    {
        foreach(SpriteRenderer spr in sprites
            .Where(sr => sr is not null && sr.enabled)
            .OrderByDescending(sr => (sr.pos - camera.pos).sqrLength))
            DrawSprite(data, spr);
    }

    public void ApplyPostEffects(BitmapData data)
    {
        foreach(PostProcessEffect effect in postProcessEffects)
            if(effect.enabled)
                effect.Apply(data);
    }

    public void DrawUnsafeGui(BitmapData data)
    {
        foreach(GuiGroup group in guiGroups)
            group.DrawUnsafeElements((byte*)data.Scan0, data.Stride, data.Width, data.Height);
    }

    public void DrawSafeGui(Bitmap bitmap)
    {
        using Graphics g = Graphics.FromImage(bitmap);

        foreach(GuiGroup group in guiGroups)
            group.DrawSafeElements(g);
    }

    public void Draw(Bitmap bitmap, BitmapData data)
    {
        DrawWalls(data);
        DrawFloorAndCeil(data);
        DrawSprites(data);
        ApplyPostEffects(data);
        DrawUnsafeGui(data);

        bitmap.UnlockBits(data);

        DrawSafeGui(bitmap);
    }

    public Bitmap Draw()
    {
        PrepareDraw(out Bitmap bitmap, out BitmapData data);
        Draw(bitmap, data);
        return bitmap;
    }

    public float GetDistanceFog(float dist)
        => MathF.Exp(fogCoefficient * dist);
    public float GetDistanceFog01(float dist01)
        => GetDistanceFog(dist01 * _fogMaxDist);


    private void DrawRow(BitmapData data, int y)
    {
        float rowDist = wallHeight * virtCenter.y / y;

        Vec2f step = rowDist * 2 * camera.plane / virtRes.x;
        Vec2f floor = camera.pos + rowDist * (camera.forward - camera.plane);

        byte* floorScan = (byte*)data.Scan0 + data.Stride*(y + virtCenter.y);
        byte* ceilScan = (byte*)data.Scan0 + data.Stride*(virtCenter.y - y - 1);
        for(int x = 0; x < virtRes.x; x++)
        {
            if(heightBuf[x] > y)
            {
                floor += step;
                floorScan += 3;
                ceilScan += 3;
                continue;
            }

            Vec2f texFrac = floor - floor.Floor();
            Vec2i floorTex = (map.floorTex.size * texFrac * map.floorTexScale).Floor() & map.floorTex.bounds;

            bool isLightTile = floor.Ceil() % lightSpacing == Vec2i.zero;
            UnsafeGraphic ceilingTex = isLightTile ? map.lightTex : map.ceilTex;
            Vec2i ceilTex = new(
                Utils.Clamp((ceilingTex.size.x * texFrac.x * map.ceilTexScale).Floor(), 0, ceilingTex.wb),
                Utils.Clamp((ceilingTex.size.y * texFrac.y * map.ceilTexScale).Floor(), 0, ceilingTex.hb));

            floor += step;

            float tileDist = (camera.pos - floor).length;
            float fog = GetDistanceFog(tileDist);
            float lightDist = 0f;
            if(lighting)
            {
                float lightDistSqr = Utils.Sqr(floor.x + .5f - (floor.x / lightSpacing).Round() * lightSpacing) + Utils.Sqr(floor.y + .5f - (floor.y / lightSpacing).Round() * lightSpacing);
                lightDist = MathF.Sqrt(lightDistSqr);
                fog = Utils.Clamp01(fog * GetDistanceFog(lightDist) * lightStrength / (1f + lightDistSqr));
            }

            float floorBrightness = map.floorLuminance * fog;
            byte* floorCol = map.floorTex.scan0 + map.floorTex.stride*floorTex.y + 3*floorTex.x;
            *floorScan++ = (byte)(*floorCol++ * floorBrightness);
            *floorScan++ = (byte)(*floorCol++ * floorBrightness);
            *floorScan++ = (byte)(*floorCol * floorBrightness);

            float ceilBrightness = map.ceilLuminance * fog;
            byte* ceilCol = ceilingTex.scan0 + ceilingTex.stride*ceilTex.y + 3*ceilTex.x;

            if(lighting && isLightTile && *ceilCol == 0xff && *(ceilCol+1) == 0xff && *(ceilCol+2) == 0xff && tileDist < 10f)
                *ceilScan++ = *ceilScan++ = *ceilScan++ = 0xff;
            else
            {
                *ceilScan++ = (byte)(*ceilCol++ * ceilBrightness);
                *ceilScan++ = (byte)(*ceilCol++ * ceilBrightness);
                *ceilScan++ = (byte)(*ceilCol * ceilBrightness);
            }
        }
    }

    private void DrawColumn(BitmapData data, int x)
    {
        Vec2f dir = camera.forward + camera.plane * (2f*x / (virtRes.x-1f) - 1f);
        Vec2i mPos = camera.pos.Floor();

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
        float normDist = Utils.Clamp01(dist / camera.maxRenderDist);
        Vec2f hitPos = camera.pos + dir * dist;

        if(depthBuf[x] < normDist || dist > camera.maxRenderDist || dist < 0f)
            return;

        depthBuf[x] = normDist;

        float height = wallHeight * virtRes.y / dist;
        int halfHeight = (height / 2f).Floor();
        int y0 = Utils.Clamp(virtCenter.y - halfHeight, 0, virtRes.y-1),
            y1 = Utils.Clamp(virtCenter.y + halfHeight, 0, virtRes.y-1);

        heightBuf[x] = (ushort)halfHeight;

        float brightness = (vert ? .75f : .5f) * GetDistanceFog(dist);
        if(lighting)
        {
            float lightDistSqr = Utils.Sqr(hitPos.x + .5f - (hitPos.x / lightSpacing).Round() * lightSpacing) + Utils.Sqr(hitPos.y + .5f - (hitPos.y / lightSpacing).Round() * lightSpacing);
            brightness = Utils.Clamp01(brightness * GetDistanceFog(MathF.Sqrt(lightDistSqr)) * lightStrength / (1f + lightDistSqr));
        }

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

    private void DrawSprite(BitmapData data, SpriteRenderer spr)
    {
        Vec2f dir = camera.forward, plane = camera.plane;

        Vec2f relPos = spr.pos - camera.pos;
        Vec2f transform = new Vec2f(dir.y*relPos.x - dir.x*relPos.y, plane.x*relPos.y - plane.y*relPos.x) / (dir.y*plane.x - dir.x*plane.y);

        if(transform.y >= camera.maxRenderDist || transform.y <= 0)
            return;

        float normDist = transform.y/camera.maxRenderDist;
        float brightness = GetDistanceFog(transform.y);

        int locX = (virtCenter.x * (1 + transform.x/transform.y)).Floor();
        Vec2i size = (spr.size * Math.Abs(virtRes.y/transform.y)).Floor();
        int drawCenter = (int)((.5f - spr.elevation/transform.y) * virtRes.y);

        int x0 = Math.Max(locX - size.x/2, 0),
            x1 = Math.Min(locX + size.x/2, virtRes.x);

        int y0 = Math.Max(drawCenter - size.y/2, 0),
            y1 = Math.Min(drawCenter + size.y/2, virtRes.y);

        float invTransformY = 1 / transform.y;
        int floorY = Math.Min((int)(virtCenter.y * (1f - invTransformY)), virtRes.y-1);
        int ceilY = Math.Max((int)(virtCenter.y * (invTransformY + 1f)), 0);

        byte* scan = (byte*)data.Scan0 + y0*data.Stride + x0*3;
        int backpaddle = (y1 - y0) * data.Stride;

        for(int x = x0; x < x1; x++)
        {
            if(normDist > depthBuf[x])
            {
                scan += 3;
                continue;
            }

            int texX = Utils.Clamp(((x - (locX - size.x/2f)) * spr.graphic.wb / size.x).Floor(), 0, spr.graphic.wb);
            byte* texScan = spr.graphic.scan0 + 4*texX;

            for(int y = y0; y < y1; y++)
            {
                bool draw = Utils.InBetweenIncl(y, floorY, ceilY);

                if(draw)
                {
                    int texY = Utils.Clamp(((y - drawCenter + size.y/2f) * spr.graphic.hb / size.y).Floor(), 0, spr.graphic.hb);
                    byte* colScan = texScan + texY*spr.graphic.stride;
                    byte alpha = *(colScan+3);

                    if(alpha == 0xff)
                    {
                        *scan = (byte)(*colScan * brightness);
                        *(scan+1) = (byte)(*(colScan+1) * brightness);
                        *(scan+2) = (byte)(*(colScan+2) * brightness);
                    }
                    else if(alpha != 0)
                    {
                        if(useFastColorBlend)
                            (*(scan+2), *(scan+1), *scan) =
                                Utils.BlendColorsCrude(
                                    *(scan+2), *(scan+1), *scan,
                                    (byte)(*(colScan+2) * brightness), (byte)(*(colScan+1) * brightness), (byte)(*colScan * brightness),
                                    alpha/255f);
                        else
                            (*(scan+2), *(scan+1), *scan) =
                                Utils.BlendColors(
                                    *(scan+2), *(scan+1), *scan,
                                    (byte)(*(colScan+2) * brightness), (byte)(*(colScan+1) * brightness), (byte)(*colScan * brightness),
                                    alpha/255f);
                    }
                }

                scan += data.Stride;
            }

            scan += 3 - backpaddle;
        }
    }
}