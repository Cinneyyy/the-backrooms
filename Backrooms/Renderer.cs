using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Backrooms.PostProcessing;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Backrooms;

public unsafe class Renderer
{
    public readonly Vec2i virtRes, physicalRes;
    public readonly Vec2i virtCenter, physicalCenter;
    public readonly Vec2i outputRes, outputLocation;
    public readonly float downscaleFactor, upscaleFactor;
    public Camera camera;
    public Input input;
    public Map map;
    public Window window;
    public readonly List<SpriteRenderer> sprites = [];
    public readonly List<TextElement> texts = [];
    public readonly List<PostProcessEffect> postProcessEffects = [];
    public float[] depthBuf;
    public bool drawIfCursorOffscreen = true;


    public Renderer(Vec2i virtRes, Vec2i physicalRes, Window window)
    {
        this.virtRes = virtRes;
        this.physicalRes = physicalRes;
        this.window = window;
        virtCenter = virtRes/2;
        physicalCenter = physicalRes/2;
        downscaleFactor = (float)virtRes.y/physicalRes.y;
        upscaleFactor = (float)physicalRes.y/virtRes.y;
        float virtRatio = (float)virtRes.x / virtRes.y;
        outputRes = new((virtRatio * physicalRes.y).Floor(), physicalRes.y);
        outputLocation = new((physicalRes.x - outputRes.x) / 2, 0);
        depthBuf = new float[virtRes.x];
    }


    public unsafe Bitmap Draw()
    {
        // TODO: Floor & Ceiling, Fisheye fix for sprites (probably requires rewriting sprite renderer)

        if(camera is null || map is null || !drawIfCursorOffscreen && input.cursorOffScreen)
            return new(1, 1);

        Array.Fill(depthBuf, 1f);
        Bitmap bitmap = new(virtRes.x, virtRes.y);
        BitmapData data = bitmap.LockBits(new(0, 0, virtRes.x, virtRes.y), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

        //DrawFloorAndCeil(data);

        for(int x = 0; x < virtRes.x; x++)
            DrawWallSegment(data, x);

        {
            sprites.Sort((a, b) => ((b.pos - camera.pos).sqrLength - (a.pos - camera.pos).sqrLength).Round());
            Vec2f dir = camera.forward;

            if(camera.fixFisheyeEffect)
                foreach(SpriteRenderer spr in sprites)
                    DrawSpriteFisheyeFixed(data, spr);
            else
                foreach(SpriteRenderer spr in sprites)
                    DrawSprite(data, spr);
        }

        foreach(PostProcessEffect effect in postProcessEffects)
            effect.Apply(data);

        bitmap.UnlockBits(data);

        if(texts is not [])
            using(Graphics g = Graphics.FromImage(bitmap))
                foreach(TextElement t in texts)
                    if(t.enabled)
                        g.DrawString(t.text, t.font, Brushes.White, t.rect);

        return bitmap;
    }

    private void DrawWallSegment(BitmapData data, int x)
    {
        Vec2f dir;
        if(camera.fixFisheyeEffect)
            dir = camera.forward - camera.plane * (2f*x / (virtRes.x-1f) - 1f);
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

        if(depthBuf[x] <= normDist || dist >= camera.maxDist || dist <= 0f)
            return;

        depthBuf[x] = normDist;

        float height = virtRes.y / dist;
        int halfHeight = (height / 2f).Floor();
        int y0 = Math.Max(virtCenter.y - halfHeight, 0),
            y1 = Math.Min(virtCenter.y + halfHeight, virtRes.y-1);

        float brightness = GetDistanceFog(normDist) * (vert ? 1f : .66f);

        UnsafeGraphic tex = map.TextureAt(mPos);
        float wallX = (vert ? (camera.pos.y + dist * dir.y) : (camera.pos.x + dist * dir.x)) % 1f;
        int texX = (wallX * (tex.w-1)).Floor();
        if(vert && dir.x > 0f || !vert && dir.y < 0f)
            texX = tex.w - texX - 1;

        float texStep = tex.h / height;
        float texPos = (y0 - virtCenter.y + halfHeight) * texStep;
        int texMask = tex.h-1;

        byte* scan = (byte*)data.Scan0 + y0*data.Stride + x*3;

        for(int y = y0; y < y1; y++)
        {
            int texY = (int)texPos & texMask;
            texPos += texStep;
            byte* texScan = tex.scan0 + texY*tex.stride + texX*3;

            *scan     = (byte)(*texScan     * brightness);
            *(scan+1) = (byte)(*(texScan+1) * brightness);
            *(scan+2) = (byte)(*(texScan+2) * brightness);

            scan += data.Stride;
        }
    }

    // TODO: Floor rendering, ceiling moves and rotates wrong along with the camera, fisheye fix toggle, fog
    public void DrawFloorAndCeil(BitmapData data)
    {
        BitmapData floorTex = map.floorTex.data, ceilTex = map.ceilTex.data;
        Vec2i floorTexSize = map.floorTex.size, ceilTexSize = map.ceilTex.size;
        Vec2i floorTexBounds = floorTexSize - Vec2i.one, ceilTexBounds = ceilTexSize - Vec2i.one;
        Vec2f dir = Vec2f.FromAngle(camera.angle);
        Vec2f plane = new Vec2f(-dir.y, dir.x) * camera.fovFactor;

        byte* ceilScan = (byte*)data.Scan0;
        byte* floorScan = (byte*)data.Scan0 + virtCenter.y * data.Stride;

        for(int y = 0; y < virtCenter.y; y++)
        {
            Vec2f lDir = dir - plane, rDir = dir + plane;

            float rowDist = (float)virtCenter.y / (virtCenter.y - y);
            Vec2f rowDistVec = new(rowDist, -rowDist);

            Vec2f floorStep = (rDir - lDir) * rowDistVec / virtRes.x;
            Vec2f floor = camera.pos + (rowDistVec * lDir);

            for(int x = 0; x < virtRes.x; x++)
            {
                Vec2i cell = floor.Round();
                Vec2i ceilTexCoord = (2f * ceilTexSize * (floor - cell)).Round() & ceilTexBounds,
                      floorTexCoord = (floorTexSize * (floor - cell)).Round() & floorTexBounds;

                byte* ceilColPtr = (byte*)ceilTex.Scan0 + ceilTexCoord.y * ceilTex.Stride + ceilTexCoord.x * 3,
                      floorColPtr = (byte*)floorTex.Scan0 + floorTexCoord.y * floorTex.Stride + floorTexCoord.x * 3;

                floor += floorStep;
                *ceilScan++ = *ceilColPtr++;
                *ceilScan++ = *ceilColPtr++;
                *ceilScan++ = *ceilColPtr;

                //*floorScan++ = *floorColPtr++;
                //*floorScan++ = *floorColPtr++;
                //*floorScan++ = *floorColPtr;
            }
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

        float locXF = (camSpace.toAngle/camera.fov + .5f) * virtRes.x - sizeF.x/2f;

        Vec2i size = sizeF.Floor();
        int locX = locXF.Floor();

        int x0 = locX - size.x/2,
            x1 = locX + size.x/2,
            xDiff = x1-x0,
            unfixedX0 = x0;
        x0 = Math.Max(x0, 0);
        x1 = Math.Min(x1, virtRes.x-1);
        int x0Change = x0 - unfixedX0;

        if(x0 >= x1)
            return;

        float normDist = dist / camera.maxDist;
        if(normDist >= 1f || normDist <= 0f)
            return;

        float brightness = GetDistanceFog(normDist);
        int y0 = virtCenter.y - size.y/2,
            y1 = virtCenter.y + size.y/2,
            yDiff = y1-y0,
            unfixedY0 = y0;
        y0 = Math.Max(y0, 0);
        y1 = Math.Min(y1, virtRes.y-1);
        int y0Change = y0 - unfixedY0;

        byte* scan = (byte*)data.Scan0 + y0*data.Stride + x0*3;
        int backpaddle = (y1-y0) * data.Stride;

        for(int x = x0; x < x1; x++)
        {
            int texX = Utils.Clamp((((float)x-x0+x0Change)/xDiff * (spr.graphic.w-1)).Floor(), 0, spr.graphic.w-1);
            byte* texScan = spr.graphic.scan0 + texX*4;

            for(int y = y0; y < y1; y++)
            {
                int texY = Utils.Clamp((((float)y-y0+y0Change)/yDiff * (spr.graphic.h-1)).Floor(), 0, spr.graphic.h-1);
                byte* colScan = texScan + texY*spr.graphic.stride;

                if(*(colScan+3) > 0x80)
                {
                    byte b = (byte)(*colScan * brightness);
                    byte g = (byte)(*(colScan+1) * brightness);
                    byte r = (byte)(*(colScan+2) * brightness);
                    *scan = b;
                    *(scan+1) = g;
                    *(scan+2) = r;
                    //*scan = (byte)(*colScan * brightness);
                    //*(scan+1) = (byte)(*(colScan+1) * brightness);
                    //*(scan+2) = (byte)(*(colScan+2) * brightness);
                }

                scan += data.Stride;
            }

            scan += 3 - backpaddle;
        }
    }

    private void DrawSpriteFisheyeFixed(BitmapData data, SpriteRenderer spr)
    {
        Vec2f dir = camera.forward, plane = -camera.plane;

        Vec2f relPos = spr.pos - camera.pos;
        Vec2f transform = new Vec2f(dir.y*relPos.x - dir.x*relPos.y, plane.x*relPos.y - plane.y*relPos.x) / (dir.y*plane.x - dir.x*plane.y);

        if(transform.y >= camera.maxDist || transform.y <= 0)
            return;

        float normDist = transform.y/camera.maxDist;
        float brightness = GetDistanceFog(normDist);

        int locX = (virtCenter.x * (1 + transform.x/transform.y)).Floor();
        Vec2i size = (spr.size * Math.Abs(virtRes.y/transform.y)).Floor();

        int x0 = Math.Max(locX - size.x/2, 0),
            x1 = Math.Min(locX + size.x/2, virtRes.x-1);
        int y0 = Math.Max(virtCenter.y - size.y/2, 0),
            y1 = Math.Min(virtCenter.y + size.y/2, virtRes.y-1);

        byte* scan = (byte*)data.Scan0 + y0*data.Stride + x0*3;
        int backpaddle = (y1 - y0) * data.Stride;

        for(int x = x0; x < x1; x++)
        {
            if(normDist > depthBuf[x])
            {
                scan += 3;
                continue;
            }

            int texX = Utils.Clamp(((x - (locX - size.x/2f)) * spr.graphic.w / size.x).Floor(), 0, spr.graphic.w);
            byte* texScan = spr.graphic.scan0 + 4*texX;

            for(int y = y0; y < y1; y++)
            {
                int texY = Utils.Clamp(((y - virtCenter.y + size.y/2f) * spr.graphic.h / size.y).Floor(), 0, spr.graphic.h);
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