using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using Backrooms.Gui;
using Backrooms.PostProcessing;
using System.Linq;

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
    public event Action dimensionsChanged;
    public bool useParallelRendering = true;


    public Vec2i virtRes { get; private set; }
    public Vec2i physRes { get; private set; }
    public Vec2i virtCenter { get; private set; }
    public Vec2i physCenter { get; private set; }
    public Vec2i outputRes { get; private set; }
    public Vec2i outputLocation { get; private set; }
    public float downscaleFactor { get; private set; }
    public float upscaleFactor { get; private set; }
    public float virtRatio { get; private set; }
    public float physRatio { get; private set; }


    public Renderer(Vec2i virtRes, Vec2i physRes, Window window)
    {
        this.window = window;
        UpdateResolution(virtRes, physRes);
    }


    public GuiGroup FindGuiGroup(string name)
        => (from g in guiGroups
            where g.name == name
            select g).FirstOrDefault();

    public void UpdateResolution(Vec2i virtRes, Vec2i physRes)
    {
        this.virtRes = virtRes;
        this.physRes = physRes;

        virtCenter = virtRes/2;
        physCenter = physRes/2;

        downscaleFactor = MathF.Min((float)virtRes.x/physRes.x, (float)virtRes.y/physRes.y);
        upscaleFactor = 1f / downscaleFactor;

        virtRatio = (float)virtRes.x / virtRes.y;
        physRatio = (float)physRes.x / physRes.y;

        if(physRatio > virtRatio)
            outputRes = new((int)(physRes.y * virtRatio), physRes.y);
        else
            outputRes = new(physRes.x, (int)(physRes.x / virtRatio));

        outputLocation = (physRes - outputRes) / 2;
        depthBuf = new float[virtRes.x];

        dimensionsChanged?.Invoke();
    }

    public unsafe Bitmap Draw()
    {
        if(camera is null || map is null || !drawIfCursorOffscreen && input.cursorOffScreen)
            return new(1, 1);

        Array.Fill(depthBuf, 1f);
        Bitmap bitmap = new(virtRes.x, virtRes.y);
        BitmapData data = bitmap.LockBits(new(0, 0, virtRes.x, virtRes.y), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

        if(useParallelRendering)
            Parallel.For(0, virtRes.x, x => DrawColumn(data, x));
        else
            for(int x = 0; x < virtRes.x; x++)
                DrawColumn(data, x);

        sprites.Sort((a, b) => ((b.pos - camera.pos).sqrLength - (a.pos - camera.pos).sqrLength).Round());
        if(camera.fixFisheyeEffect)
            foreach(SpriteRenderer spr in sprites)
                DrawSpriteFisheyeFixed(data, spr);
        else
            foreach(SpriteRenderer spr in sprites)
                DrawSprite(data, spr);

        foreach(PostProcessEffect effect in postProcessEffects)
            effect.Apply(data);

        foreach(GuiGroup group in guiGroups)
            group.DrawUnsafeElements((byte*)data.Scan0, data.Stride, data.Width, data.Height);

        bitmap.UnlockBits(data);

        using(Graphics g = Graphics.FromImage(bitmap))
            foreach(GuiGroup group in guiGroups)
                group.DrawSafeElements(g);

        return bitmap;
    }

    private void DrawColumn(BitmapData data, int x)
    {
        Vec2f dir;
        if(camera.fixFisheyeEffect)
            dir = camera.forward + camera.plane * (2f*x / (virtRes.x-1f) - 1f);
        else
            dir = Vec2f.FromAngle(camera.fov * (x / (virtRes.x-1f) - .5f) + camera.angle);

        Vec2i mPos = camera.pos.Floor();

        Vec2f deltaDist = new(
            dir.x == 0f ? float.MaxValue : MathF.Abs(1f / dir.x),
            dir.y == 0f ? float.MaxValue : MathF.Abs(1f / dir.y));

        Vec2f sideDist = new(
            deltaDist.x * (dir.x < 0f ? (camera.pos.x - mPos.x) : (mPos.x + 1f - camera.pos.x)),
            deltaDist.y * (dir.y < 0f ? (camera.pos.y - mPos.y) : (mPos.y + 1f - camera.pos.y)));

        Vec2i step = new(MathF.Sign(dir.x), MathF.Sign(dir.y));

        bool hit = false, vert = false;

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

            if(!map.InBounds(mPos))
                return;

            if(map[mPos] != Tile.Empty)
                hit = true;
        }

        Vec2f hitPos = camera.pos + sideDist;

        float dist = vert ? (sideDist.x - deltaDist.x) : (sideDist.y - deltaDist.y);
        float normDist = Utils.Clamp01(dist / camera.maxDist);

        bool drawWall = true;
        if(depthBuf[x] <= normDist || dist >= camera.maxDist || dist <= 0f)
            drawWall = false;

        depthBuf[x] = normDist;

        float height = virtRes.y / dist;
        int halfHeight = (height / 2f).Floor();
        int y0 = Utils.Clamp(virtCenter.y - halfHeight, 0, virtRes.y-1),
            y1 = Utils.Clamp(virtCenter.y + halfHeight, 0, virtRes.y-1);

        float brightness = GetDistanceFog(normDist) * (vert ? .75f : .5f);

        UnsafeGraphic tex = map.TextureAt(mPos);
        float wallX = (vert ? (camera.pos.y + dist * dir.y) : (camera.pos.x + dist * dir.x)) % 1f;
        int texX = (wallX * ((tex?.w ?? 1) - 1)).Floor();
        if(vert && dir.x > 0f || !vert && dir.y < 0f)
            texX = tex.w - texX - 1;

        float texStep = tex.h / height;
        float texPos = (y0 - virtCenter.y + halfHeight) * texStep;
        int texMask = tex.h-1;

        byte* scan = (byte*)data.Scan0 + y0*data.Stride + x*3;

        if(drawWall)
            for(int y = y0; y <= y1; y++)
            {
                int texY = (int)texPos & texMask;
                texPos += texStep;
                byte* texScan = tex.scan0 + texY*tex.stride + texX*3;

                *scan     = (byte)(*texScan     * brightness);
                *(scan+1) = (byte)(*(texScan+1) * brightness);
                *(scan+2) = (byte)(*(texScan+2) * brightness);

                scan += data.Stride;
            }
        else
            scan += (y1-y0+1)*data.Stride;

        Vec2f floorWall;
        if(vert)
            floorWall = new(dir.x > 0 ? mPos.x : mPos.x + 1, mPos.y + wallX);
        else
            floorWall = new(mPos.x + wallX, dir.y > 0 ? mPos.y : mPos.y + 1f);

        float distWall = dist, distPlayer = 0f, currDist;
        byte* ceilScan = (byte*)data.Scan0 + (virtRes.y-y1-1)*data.Stride + x*3;

        for(int y = y1+1; y <= virtRes.y; y++)
        {
            currDist = virtRes.y / (2f * y - virtRes.y);
            float weight = (currDist - distPlayer) / (distWall - distPlayer);

            Vec2f currFloor = weight * floorWall + (Vec2f.one - new Vec2f(weight)) * camera.pos;

            Vec2i floorTex = (currFloor * map.floorTex.size * map.floorTexScale).Round() % map.floorTex.size;
            Vec2i ceilTex = (currFloor * map.ceilTex.size * map.ceilTexScale).Round() % map.ceilTex.size;

            (byte r, byte g, byte b) floorCol = map.floorTex.GetPixelRgb(floorTex.x, floorTex.y);
            (byte r, byte g, byte b) ceilCol = map.ceilTex.GetPixelRgb(ceilTex.x, ceilTex.y);

            float fog = GetDistanceFog(Utils.Clamp01((camera.pos - currFloor).length / camera.maxDist));
            float floorBrightness = map.floorLuminance * fog;
            float ceilBrightness = map.ceilLuminance * fog;

            if(y != virtRes.y)
            {
                *scan = (byte)(floorCol.b * floorBrightness);
                *(scan+1) = (byte)(floorCol.g * floorBrightness);
                *(scan+2) = (byte)(floorCol.r * floorBrightness);
            }

            if(*ceilScan == 0)
            {
                *ceilScan = (byte)(ceilCol.b * ceilBrightness);
                *(ceilScan+1) = (byte)(ceilCol.g * ceilBrightness);
                *(ceilScan+2) = (byte)(ceilCol.r * ceilBrightness);
            }

            scan += data.Stride;
            ceilScan -= data.Stride;
        }
    }

    private void DrawSprite(BitmapData data, SpriteRenderer spr)
    {
        Vec2f dir = camera.forward;

        Vec2f relPos = spr.pos - camera.pos;
        Vec2f camSpace = new(
                relPos.x * dir.x + relPos.y * dir.y,
                -relPos.x * dir.y + relPos.y * dir.x);

        float dist = relPos.length;
        if(dist >= camera.maxDist || dist <= 0f)
            return;

        Vec2f sizeF = spr.size * virtRes.y / dist;

        if(sizeF.x <= 0f || sizeF.y <= 0f)
            return;

        Vec2i size = sizeF.Floor();
        Vec2i hSize = size/2;
        int locX = ((camSpace.toAngle/camera.fov + .5f) * (virtRes.x-1) - sizeF.x/2f).Floor();

        int x0 = Math.Max(locX - hSize.x, 0),
            x1 = Math.Min(locX + hSize.x, virtRes.x);

        if(x0 >= x1)
            return;

        float normDist = dist / camera.maxDist;
        if(normDist >= 1f || normDist <= 0f)
            return;

        float brightness = GetDistanceFog(normDist);
        int y0 = Math.Max(virtCenter.y - hSize.y, 0),
            y1 = Math.Min(virtCenter.y + hSize.y, virtRes.y);

        byte* scan = (byte*)data.Scan0 + y0*data.Stride + x0*3;
        int backpaddle = (y1-y0) * data.Stride;

        Vec2f texOffset = new(hSize.x - locX, hSize.y - virtCenter.y);
        Vec2f texMappingFactor = (Vec2f)spr.graphic.bounds / size;
        for(int x = x0; x < x1; x++)
        {
            if(normDist > depthBuf[x])
            {
                scan += 3;
                continue;
            }

            int texX = Utils.Clamp(((x + texOffset.x) * texMappingFactor.x).Floor(), 0, spr.graphic.wb);
            byte* texScan = spr.graphic.scan0 + texX*4;

            for(int y = y0; y < y1; y++)
            {
                int texY = Utils.Clamp(((y + texOffset.y) * texMappingFactor.y).Floor(), 0, spr.graphic.hb);
                byte* colScan = texScan + texY*spr.graphic.stride;

                if(*(colScan+3) > 0x80)
                {
                    *scan = (byte)(*colScan * brightness);
                    *(scan+1) = (byte)(*(colScan+1) * brightness);
                    *(scan+2) = (byte)(*(colScan+2) * brightness);
                }

                scan += data.Stride;
            }

            scan += 3 - backpaddle;
        }
    }

    private void DrawSpriteFisheyeFixed(BitmapData data, SpriteRenderer spr)
    {
        Vec2f dir = camera.forward, plane = camera.plane;

        Vec2f relPos = spr.pos - camera.pos;
        Vec2f transform = new Vec2f(dir.y*relPos.x - dir.x*relPos.y, plane.x*relPos.y - plane.y*relPos.x) / (dir.y*plane.x - dir.x*plane.y);

        if(transform.y >= camera.maxDist || transform.y <= 0)
            return;

        float normDist = transform.y/camera.maxDist;
        float brightness = GetDistanceFog(normDist);

        int locX = (virtCenter.x * (1 + transform.x/transform.y)).Floor();
        Vec2i size = (spr.size * Math.Abs(virtRes.y/transform.y)).Floor();

        int x0 = Math.Max(locX - size.x/2, 0),
            x1 = Math.Min(locX + size.x/2, virtRes.x);

        int y0 = Math.Max(virtCenter.y - size.y/2, 0),
            y1 = Math.Min(virtCenter.y + size.y/2, virtRes.y);

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
                int texY = Utils.Clamp(((y - virtCenter.y + size.y/2f) * spr.graphic.hb / size.y).Floor(), 0, spr.graphic.hb);
                byte* colScan = texScan + texY*spr.graphic.stride;

                if(*(colScan+3) > 0x80)
                {
                    *scan = (byte)(*colScan * brightness);
                    *(scan+1) = (byte)(*(colScan+1) * brightness);
                    *(scan+2) = (byte)(*(colScan+2) * brightness);
                }

                scan += data.Stride;
            }

            scan += 3 - backpaddle;
        }
    }


    public static float GetDistanceFog(float dist01)
        => .75f * Utils.Sqr(1f - dist01);
}