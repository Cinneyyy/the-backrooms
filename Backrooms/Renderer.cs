using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using Backrooms.Gui;
using Backrooms.PostProcessing;
using Microsoft.VisualBasic.Logging;

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
    public int[] heightBuf;
    public event Action dimensionsChanged;
    public bool useParallelRendering = true;
    public float wallHeight = 1f;

    private readonly Comparison<SpriteRenderer> sprComparison;


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


    public Renderer(Vec2i virtRes, Vec2i physRes, Window window)
    {
        this.window = window;
        UpdateResolution(virtRes, physRes);

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
        heightBuf = new int[virtRes.x];

        dimensionsChanged?.Invoke();
    }

    public unsafe Bitmap Draw(DrawParams drawParams = DrawParams.All)
    {
        if(camera is null || map is null || !drawIfCursorOffscreen && input.cursorOffScreen)
            return new(1, 1);

        Array.Fill(depthBuf, 1f);
        Bitmap bitmap = new(virtRes.x, virtRes.y);
        BitmapData data = bitmap.LockBits(new(0, 0, virtRes.x, virtRes.y), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

        if((drawParams & DrawParams.Columns) != 0)
            if(useParallelRendering)
                Parallel.For(0, virtRes.x, x => DrawColumn(data, x));
            else
                for(int x = 0; x < virtRes.x; x++)
                    DrawColumn(data, x);

        if((drawParams & DrawParams.FloorAndCeil) != 0)
            if(useParallelRendering)
                Parallel.For(0, virtCenter.y, y => DrawFloorAndCeil(data, y));
            else
                for(int y = 0; y < virtCenter.y; y++)
                    DrawFloorAndCeil(data, y);

        if((drawParams & DrawParams.Sprites) != 0)
            foreach(SpriteRenderer spr in sprites.Where(sr => sr is not null).OrderByDescending(sr => (sr.pos - camera.pos).sqrLength))
                if(spr.enabled)
                    DrawSprite(data, spr);

        if((drawParams & DrawParams.PostEffects) != 0)
            foreach(PostProcessEffect effect in postProcessEffects)
                effect.Apply(data);

        if((drawParams & DrawParams.Gui) != 0)
            foreach(GuiGroup group in guiGroups)
                group.DrawUnsafeElements((byte*)data.Scan0, data.Stride, data.Width, data.Height);

        bitmap.UnlockBits(data);

        if((drawParams & DrawParams.Gui) != 0)
            using(Graphics g = Graphics.FromImage(bitmap))
                foreach(GuiGroup group in guiGroups)
                    group.DrawSafeElements(g);

        return bitmap;
    }

    private unsafe void DrawFloorAndCeil(BitmapData data, int y)
    {
        float rowDist = wallHeight * virtCenter.y / y;

        Vec2f step = rowDist * 2 * camera.plane / virtRes.x;
        Vec2f floor = camera.pos + rowDist * (camera.forward - camera.plane);

        byte* floorScan = (byte*)data.Scan0 + data.Stride*(y + virtCenter.y);
        byte* ceilScan = (byte*)data.Scan0 + data.Stride*(virtCenter.y - y - 1);
        for(int x = 0; x < virtRes.x; x++)
        {
            if(heightBuf[x] < y)
            {
                floor += step;
                floorScan += 3;
                ceilScan += 3;
            }

            Vec2f texFrac = floor - floor.Floor();
            Vec2i floorTex = (map.floorTex.size * texFrac * map.floorTexScale).Floor() & map.floorTex.bounds;
            Vec2i ceilTex = new(
                Utils.Clamp((map.ceilTex.size.x * texFrac.x * map.ceilTexScale).Floor(), 0, map.ceilTex.wb),
                Utils.Clamp((map.ceilTex.size.y * texFrac.y * map.ceilTexScale).Floor(), 0, map.ceilTex.hb));

            floor += step;

            float fog = GetDistanceFog(Utils.Clamp01((camera.pos - floor).length / camera.maxRenderDist));

            float floorBrightness = map.floorLuminance * fog;
            byte* floorCol = map.floorTex.scan0 + map.floorTex.stride*floorTex.y + 3*floorTex.x;
            *floorScan++ = (byte)(*floorCol++ * floorBrightness);
            *floorScan++ = (byte)(*floorCol++ * floorBrightness);
            *floorScan++ = (byte)(*floorCol * floorBrightness);

            float ceilBrightness = map.ceilLuminance * fog;
            byte* ceilCol = map.ceilTex.scan0 + map.ceilTex.stride*ceilTex.y + 3*ceilTex.x;
            *ceilScan++ = (byte)(*ceilCol++ * ceilBrightness);
            *ceilScan++ = (byte)(*ceilCol++ * ceilBrightness);
            *ceilScan++ = (byte)(*ceilCol * ceilBrightness);
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
        const int max_steps = 256;

        while(!hit)
        {
            if(sideDist.x < sideDist.y)
            {
                sideDist.x += deltaDist.x;
                mPos.x += step.x;
                vert = true;
            }
            else
            {
                sideDist.y += deltaDist.y;
                mPos.y += step.y;
                vert = false;
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

        heightBuf[x] = halfHeight;

        float brightness = (vert ? .75f : .5f) * GetDistanceFog(normDist);
        //const float light_spacing = 20f, light_strength = 2f;
        //float lightDistSqr = Utils.Sqr(hitPos.x - (hitPos.x / light_spacing).Round() * light_spacing) + Utils.Sqr(hitPos.y - (hitPos.y / light_spacing).Round() * light_spacing);
        //brightness = Utils.Clamp01(brightness * light_strength / (1f + lightDistSqr));

        UnsafeGraphic tex = map.TextureAt(mPos);
        float wallX = (vert ? (camera.pos.y + dist * dir.y) : (camera.pos.x + dist * dir.x)) % 1f;
        int texX = (wallX * ((tex?.w ?? 1) - 1)).Floor();
        if(vert && dir.x > 0f || !vert && dir.y < 0f)
            texX = tex.w - texX - 1;

        float texStep = tex.h / height;
        float texPos = (y0 - virtCenter.y + halfHeight) * texStep;
        int texMask = tex.h-1;

        byte* scan = (byte*)data.Scan0 + y0*data.Stride + x*3;

        for(int y = y0; y <= y1; y++)
        {
            int texY = (int)texPos & texMask;
            texPos += texStep;
            byte* texScan = tex.scan0 + texY*tex.stride + texX*3;

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
        float brightness = GetDistanceFog(normDist);

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

                    if(*(colScan+3) > 0x80)
                    {
                        *scan = (byte)(*colScan * brightness);
                        *(scan+1) = (byte)(*(colScan+1) * brightness);
                        *(scan+2) = (byte)(*(colScan+2) * brightness);
                    }
                }

                scan += data.Stride;
            }

            scan += 3 - backpaddle;
        }
    }


    public static float GetDistanceFog(float dist01)
        => .75f * Utils.Sqr(1f - dist01);
}