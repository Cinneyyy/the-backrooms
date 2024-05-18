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
        outputRes = new((int)(virtRatio * physicalRes.y), physicalRes.y);
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

        DrawFloorAndCeil(data);

        DrawSprites(data);

        for(int x = 0; x < virtRes.x; x++)
            DrawWallSegment(data, in x);

        foreach(PostProcessEffect effect in postProcessEffects)
            effect.Apply(data);

        bitmap.UnlockBits(data);

        if(texts is not [])
        {
            using(Graphics g = Graphics.FromImage(bitmap))
                foreach(TextElement t in texts)
                    g.DrawString(t.text, t.font, Brushes.White, t.rect);
        }

        return bitmap;
    }

    // TODO: Floor rendering, ceiling moves with you in y-direction (fix that), fisheye fix toggle, fog
    public void DrawFloorAndCeil(BitmapData data)
    {
        BitmapData floorTex = map.floorTex.data, ceilTex = map.ceilTex.data;
        Vec2i floorTexSize = map.floorTex.size, ceilTexSize = map.ceilTex.size;
        Vec2i floorTexBounds = floorTexSize - Vec2i.one, ceilTexBounds = ceilTexSize - Vec2i.one;
        Vec2f dir = camera.forward;
        Vec2f plane = camera.plane;

        byte* ceilScan = (byte*)data.Scan0;
        byte* floorScan = (byte*)data.Scan0 + virtCenter.y * data.Stride;

        for(int y = 0; y < virtCenter.y; y++)
        {
            Vec2f lDir = dir - plane, rDir = dir + plane;

            float rowDist = (float)virtCenter.y / (y - virtCenter.y);

            Vec2f floorStep = (rDir - lDir) * rowDist / virtRes.x;
            Vec2f floor = camera.pos + (rowDist * lDir);

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
        sprites.Sort((a, b) => (int)MathF.Round((b.pos - camera.pos).sqrLength - (a.pos - camera.pos).sqrLength));
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
                DrawWallSegment(data, in x);

            float dist01 = dist / camera.maxDist;
            if(dist01 >= 1f || dist01 <= 0f)
                continue;

            float fog = 1f; //GetDistanceFog(dist01);
            if(spr.hasTransparency)
                DrawBitmapCutout24(data, spr.lockedImage.data, loc.x, loc.y, size.x, size.y, fog);
            else
                DrawBitmap24(data, spr.lockedImage.data, loc.x, loc.y, size.x, size.y, fog);

            FillDepthBufRange(x0, x1, dist01);
        }
    }

    private void DrawWallSegment(BitmapData data, in int x)
    {
        float baseAngle = camera.fov * (x / (virtRes.x-1f) - .5f);
        float rayAngle = Utils.NormAngle(camera.angle + baseAngle);
        Vec2f dir = Vec2f.FromAngle(rayAngle);
        Vec2i iPos = Map.Round(camera.pos);

        Vec2f deltaDist = new(
            dir.x == 0f ? float.MaxValue : MathF.Abs(1f / dir.x),
            dir.y == 0f ? float.MaxValue : MathF.Abs(1f / dir.y));

        Vec2f sideDist = new(
            deltaDist.x * (dir.x < 0f ? (camera.pos.x - iPos.x) : (iPos.x + 1f - camera.pos.x)),
            deltaDist.y * (dir.y < 0f ? (camera.pos.y - iPos.y) : (iPos.y + 1f - camera.pos.y)));

        Vec2i step = new(Math.Sign(dir.x), Math.Sign(dir.y));

        (Tile tile, bool vert) hit = (Tile.Empty, false);
        while(hit.tile == Tile.Empty)
        {
            if(sideDist.x < sideDist.y)
            {
                sideDist.x += deltaDist.x;
                iPos.x += step.x;
                hit.vert = true;
            }
            else
            {
                sideDist.y += deltaDist.y;
                iPos.y += step.y;
                hit.vert = false;
            }

            if(!map.InBounds(iPos))
                return;

            hit.tile = map[iPos];
        }

        Vec2f hitPos = camera.pos + sideDist;
        float euclideanDist = hit.vert ? sideDist.x - deltaDist.x : sideDist.y - deltaDist.y;
        float dist = (camera.fixFisheyeEffect ? MathF.Cos(baseAngle) : 1f) * euclideanDist;
        float dist01 = Utils.Clamp01(dist / camera.maxDist);

        if(dist > camera.maxDist || dist == 0f || depthBuf[x] < dist01)
            return;

        depthBuf[x] = dist01;

        float heightF = virtRes.y / dist / 2f;
        float brightness = (hit.vert ? 1f : .75f);// * GetDistanceFogUnclamped(dist01);

        // TODO: Better fix for fisheye effect
        LockedBitmap tex = map.textures[(int)hit.tile];
        float wallX = (hit.vert ? camera.pos.y + euclideanDist * dir.y : camera.pos.x + euclideanDist * dir.x) % 1f;
        int texX = (int)(wallX * tex.data.Width);
        if(hit.vert && dir.x > 0f || !hit.vert && dir.y < 0f)
            texX = tex.data.Width - texX - 1;

        byte* texPtr = (byte*)tex.data.Scan0 + texX*3;

        int height = (int)heightF,
            y0 = Math.Max(0, virtCenter.y - height),
            y1 = Math.Min(virtRes.y-1, virtCenter.y + height),
            tMin = y0 - virtCenter.y + height,
            tMax = (int)(2f * heightF);
        byte* outPtr = (byte*)data.Scan0 + x*3 + y0*data.Stride;
        for(int y = y0, i = tMin; y < y1; y++, i++)
        {
            byte* texRow = texPtr + (int)((float)i / tMax * tex.data.Height) * tex.data.Stride;
            *(outPtr) = (byte)(*(texRow) * brightness);
            *(outPtr+1) = (byte)(*(texRow+1) * brightness);
            *(outPtr+2) = (byte)(*(texRow+2) * brightness);
            outPtr += data.Stride;
        }
    }

    private void FillDepthBuf(int x, int w, float depth)
        => FillDepthBufRange(x, x+w, depth);

    private void FillDepthBufRange(int x0, int x1, float depth)
    {
        x0 = Utils.Clamp(x0, 0, virtRes.x);
        x1 = Utils.Clamp(x1, 0, virtRes.x);
        for(int x = x0; x < x1; x++)
            depthBuf[x] = depth;
    }

    private unsafe void SetPixel24(BitmapData data, int x, int y, Color32 col)
    {
        byte* ptr = (byte*)data.Scan0 + data.Stride * y + 3 * x;
        *ptr = col.b;
        *(ptr+1) = col.g;
        *(ptr+2) = col.r;
    }

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
                        byte* uvPtr = srcPtr + ((int)((float)y/sy * (sh-1)) * srcImage.Stride) + ((int)((float)x/sx * (sw-1)) * 3);
                        *dstPtr = *uvPtr;
                        *(dstPtr+1) = *(uvPtr+1);
                        *(dstPtr+2) = *(uvPtr+2);
                    }

                    dstPtr += 3;
                }
            }
    }
    private unsafe void DrawBitmap24(BitmapData dstImage, BitmapData srcImage, int lx, int ly, int sx, int sy, in float colMulR, in float colMulG, in float colMulB)
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
                        byte* uvPtr = srcPtr + ((int)((float)y/sy * (sh-1)) * srcImage.Stride) + ((int)((float)x/sx * (sw-1)) * 3);
                        *dstPtr = (byte)(*uvPtr * colMulB);
                        *(dstPtr+1) = (byte)(*(uvPtr+1) * colMulG);
                        *(dstPtr+2) = (byte)(*(uvPtr+2) * colMulR);
                    }

                    dstPtr += 3;
                }
            }
    }
    private unsafe void DrawBitmap24(BitmapData dstImage, BitmapData srcImage, int lx, int ly, int sx, int sy, float colMul = 1f)
        => DrawBitmap24(dstImage, srcImage, lx, ly, sx, sy, in colMul, in colMul, in colMul);

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
                        byte* uvPtr = srcPtr + ((int)((float)y/sy * (sh-1)) * srcImage.Stride) + ((int)((float)x/sx * (sw-1)) * 4);

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
    private unsafe void DrawBitmapCutout24(BitmapData dstImage, BitmapData srcImage, int lx, int ly, int sx, int sy, in float colMulR, in float colMulG, in float colMulB)
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
                        byte* uvPtr = srcPtr + ((int)((float)y/sy * (sh-1)) * srcImage.Stride) + ((int)((float)x/sx * (sw-1)) * 4);

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
    private unsafe void DrawBitmapCutout24(BitmapData dstImage, BitmapData srcImage, int lx, int ly, int sx, int sy, float colMul = 1f)
        => DrawBitmapCutout24(dstImage, srcImage, lx, ly, sx, sy, in colMul, in colMul, in colMul);


    public static float GetDistanceFogUnclamped(float dist01)
        => .75f * Utils.Sqr(1f - dist01);

    public static float GetDistanceFog(float dist01)
        => GetDistanceFogUnclamped(Utils.Clamp01(dist01));
}