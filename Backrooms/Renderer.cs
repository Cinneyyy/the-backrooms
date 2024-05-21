using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Backrooms.PostProcessing;

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

        DrawSprites(data);

        for(int x = 0; x < virtRes.x; x++)
            DrawWallSegment(data, x);

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


    private void DrawSprites(BitmapData data)
    {
        sprites.Sort((a, b) => ((b.pos - camera.pos).sqrLength - (a.pos - camera.pos).sqrLength).Round());
        Vec2f camDir = camera.forward;

        foreach(SpriteRenderer spr in sprites)
        {
            Vec2f camToSpr = spr.pos - camera.pos;
            Vec2f camSpace = new(
                camToSpr.x * camDir.x + camToSpr.y * camDir.y,
                -camToSpr.x * camDir.y + camToSpr.y * camDir.x);

            float dist = camToSpr.length;
            if(dist == 0f)
                continue;

            float sprAngle = camSpace.toAngleRaw;
            float hFov = camera.fov/2f;

            Vec2f sizeF = new(virtRes.y / dist * spr.size.y);
            sizeF.x *= spr.size.x/spr.size.y;

            if(sizeF.x <= 0f || sizeF.y <= 0f)
                continue;

            // if(camera.fixFisheyeEffect) ...;
            Vec2f locF = new((sprAngle/camera.fov + .5f) * virtRes.x - sizeF.x/2f, (virtRes.y - sizeF.y) / 2f);

            Vec2i size = sizeF.Round();
            Vec2i loc = locF.Round();

            int x0 = Math.Max(0, loc.x), x1 = Math.Min(virtRes.x-1, loc.x + size.x);

            if(x0 >= x1)
                continue;

            for(int x = x0; x < x1; x++)
                DrawWallSegment(data, x);

            float dist01 = dist / camera.maxDist;
            if(dist01 >= 1f || dist01 <= 0f)
                continue;

            float fog = GetDistanceFog(dist01);
            if(spr.hasTransparency)
                DrawBitmapCutout24(data, spr.lockedImage.data, loc.x, loc.y, size.x, size.y, fog, fog, fog);
            else
                DrawBitmap24(data, spr.lockedImage.data, loc.x, loc.y, size.x, size.y, fog, fog, fog);

            for(int x = x0; x < x1; x++)
                depthBuf[x] = dist01;
        }
    }

    private void DrawWallSegment(BitmapData data, int x)
    {
        Vec2f dir;
        if(camera.fixFisheyeEffect)
        {
            float screenX = 2f*x / (virtRes.x-1f) - 1f;
            dir = camera.forward - camera.plane * screenX;
        }
        else
        {
            float angle = camera.fov * (x / (virtRes.x-1f) - .5f);
            dir = Vec2f.FromAngle(angle + camera.angle);
        }

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

        if(dist >= camera.maxDist || dist <= 0f)
            return;

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

    #region Bitmap drawing
    private unsafe void DrawBitmap24(BitmapData dstImage, BitmapData srcImage, int lx, int ly, int sx, int sy)
    {
        Assert(dstImage.PixelFormat == PixelFormat.Format24bppRgb && srcImage.PixelFormat == PixelFormat.Format24bppRgb, "Both input of DrawBitmap24 must have PixelFormat.Format24BppRgb");

        byte* dstPtr = (byte*)dstImage.Scan0 + dstImage.Stride * ly + 3 * lx;
        byte* srcPtr = (byte*)srcImage.Scan0;

        int dw = dstImage.Width, dh = dstImage.Height;
        int sw = srcImage.Width, sh = srcImage.Height;

        for(int y = 0; y < sy; y++)
            if(y + ly is int dstY && dstY >= 0 && dstY < dh)
            {
                dstPtr = (byte*)dstImage.Scan0 + dstImage.Stride * dstY + 3 * lx;

                for(int x = 0; x < sx; x++)
                {
                    if(x + lx is int dstX && dstX >= 0 && dstX < dw)
                    {
                        byte* uvPtr = srcPtr + (((float)y/sy * (sh-1)).Floor() * srcImage.Stride) + (((float)x/sx * (sw-1)).Floor() * 3);
                        *dstPtr = *uvPtr;
                        *(dstPtr+1) = *(uvPtr+1);
                        *(dstPtr+2) = *(uvPtr+2);
                    }

                    dstPtr += 3;
                }
            }
    }
    private unsafe void DrawBitmap24(BitmapData dstImage, BitmapData srcImage, int lx, int ly, int sx, int sy, float colMulR, float colMulG, float colMulB)
    {
        Assert(dstImage.PixelFormat == PixelFormat.Format24bppRgb && srcImage.PixelFormat == PixelFormat.Format24bppRgb, "Both input of DrawBitmap24 must have PixelFormat.Format24BppRgb");

        byte* dstPtr = (byte*)dstImage.Scan0 + dstImage.Stride * ly + 3 * lx;
        byte* srcPtr = (byte*)srcImage.Scan0;

        int dw = dstImage.Width, dh = dstImage.Height;
        int sw = srcImage.Width, sh = srcImage.Height;

        for(int y = 0; y < sy; y++)
            if(y + ly is int dstY && dstY >= 0 && dstY < dh)
            {
                dstPtr = (byte*)dstImage.Scan0 + dstImage.Stride * dstY + 3 * lx;

                for(int x = 0; x < sx; x++)
                {
                    if(x + lx is int dstX && dstX >= 0 && dstX < dw)
                    {
                        byte* uvPtr = srcPtr + (((float)y/sy * (sh-1)).Floor() * srcImage.Stride) + (((float)x/sx * (sw-1)).Floor() * 3);
                        *dstPtr = (byte)(*uvPtr * colMulB);
                        *(dstPtr+1) = (byte)(*(uvPtr+1) * colMulG);
                        *(dstPtr+2) = (byte)(*(uvPtr+2) * colMulR);
                    }

                    dstPtr += 3;
                }
            }
    }

    private unsafe void DrawBitmapCutout24(BitmapData dstImage, BitmapData srcImage, int lx, int ly, int sx, int sy)
    {
        Assert(dstImage.PixelFormat == PixelFormat.Format24bppRgb && srcImage.PixelFormat == PixelFormat.Format32bppArgb, "Inputs of DrawBitmapCutout24 must have PixelFormat.Format24BppRgb and PixelFormat.Format32BppRgb respectively");

        byte* dstPtr = (byte*)dstImage.Scan0 + dstImage.Stride * ly + 3 * lx;
        byte* srcPtr = (byte*)srcImage.Scan0;

        int dw = dstImage.Width, dh = dstImage.Height;
        int sw = srcImage.Width, sh = srcImage.Height;

        for(int y = 0; y < sy; y++)
            if(y + ly is int dstY && dstY >= 0 && dstY < dh)
            {
                dstPtr = (byte*)dstImage.Scan0 + dstImage.Stride * dstY + 3 * lx;

                for(int x = 0; x < sx; x++)
                {
                    if(x + lx is int dstX && dstX >= 0 && dstX < dw)
                    {
                        byte* uvPtr = srcPtr + (((float)y/sy * (sh-1)).Floor() * srcImage.Stride) + (((float)x/sx * (sw-1)).Floor() * 4);

                        if(*(uvPtr+3) > 0x7f)
                        {
                            *dstPtr = *uvPtr;
                            *(dstPtr+1) = *(uvPtr+1);
                            *(dstPtr+2) = *(uvPtr+2);
                        }
                    }

                    dstPtr += 3;
                }
            }
    }
    private unsafe void DrawBitmapCutout24(BitmapData dstImage, BitmapData srcImage, int lx, int ly, int sx, int sy, float colMulR, float colMulG, float colMulB)
    {
        Assert(dstImage.PixelFormat == PixelFormat.Format24bppRgb && srcImage.PixelFormat == PixelFormat.Format32bppArgb, "Inputs of DrawBitmapCutout24 must have PixelFormat.Format24BppRgb and PixelFormat.Format32BppRgb respectively");

        byte* dstPtr = (byte*)dstImage.Scan0 + dstImage.Stride * ly + 3 * lx;
        byte* srcPtr = (byte*)srcImage.Scan0;

        int dw = dstImage.Width, dh = dstImage.Height;
        int sw = srcImage.Width, sh = srcImage.Height;

        for(int y = 0; y < sy; y++)
            if(y + ly is int dstY && dstY >= 0 && dstY < dh)
            {
                dstPtr = (byte*)dstImage.Scan0 + dstImage.Stride * dstY + 3 * lx;

                for(int x = 0; x < sx; x++)
                {
                    if(x + lx is int dstX && dstX >= 0 && dstX < dw)
                    {
                        byte* uvPtr = srcPtr + (((float)y/sy * (sh-1)).Floor() * srcImage.Stride) + (((float)x/sx * (sw-1)).Floor() * 4);

                        if(*(uvPtr+3) > 0x7f)
                        {
                            *dstPtr = (byte)(*uvPtr * colMulB);
                            *(dstPtr+1) = (byte)(*(uvPtr+1) * colMulG);
                            *(dstPtr+2) = (byte)(*(uvPtr+2) * colMulR);
                        }
                    }

                    dstPtr += 3;
                }
            }
    }
    #endregion


    public static float GetDistanceFog(float dist01)
        => .75f * Utils.Sqr(1f - dist01);
}