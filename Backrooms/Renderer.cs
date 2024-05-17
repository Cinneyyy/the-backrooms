using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Backrooms.PostProcessing;

namespace Backrooms;

public unsafe class Renderer
{
    public readonly Vec2i virtRes, physicalRes;
    public readonly Vec2i virtualCenter, physicalCenter;
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


    public Renderer(Vec2i virtualRes, Vec2i physicalRes, Window window)
    {
        this.virtRes = virtualRes;
        this.physicalRes = physicalRes;
        this.window = window;
        virtualCenter = virtualRes/2;
        physicalCenter = physicalRes/2;
        downscaleFactor = (float)virtualRes.y/physicalRes.y;
        upscaleFactor = (float)physicalRes.y/virtualRes.y;
        float virtRatio = (float)virtualRes.x / virtualRes.y;
        outputRes = new((int)(virtRatio * physicalRes.y), physicalRes.y);
        outputLocation = new((physicalRes.x - outputRes.x) / 2, 0);
        depthBuf = new float[virtualRes.x];
    }


    public unsafe Bitmap Draw()
    {
        if(camera is null || map is null || !drawIfCursorOffscreen && input.cursorOffScreen)
            return new(1, 1);

        Array.Fill(depthBuf, 1f);
        Bitmap bitmap = new(virtRes.x, virtRes.y);
        BitmapData data = bitmap.LockBits(new(0, 0, virtRes.x, virtRes.y), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

        sprites.Sort((a, b) => (int)MathF.Round((b.pos - camera.pos).sqrLength - (a.pos - camera.pos).sqrLength));
        Vec2f camDir = Vec2f.FromAngle(camera.angle);

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

            // TODO: if(camera.fixFisheyeEffect) ...; but I do not know how
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

            float fog = GetDistanceFog(dist01);
            if(spr.hasTransparency)
                DrawBitmapCutout24(data, spr.lockedImage.data, loc.x, loc.y, size.x, size.y, fog);
            else
                DrawBitmap24(data, spr.lockedImage.data, loc.x, loc.y, size.x, size.y, fog);

            FillDepthBufRange(x0, x1, dist01);
        }

        for(int x = 0; x < virtRes.x; x++)
            DrawWallSegment(data, in x);

        foreach(PostProcessEffect effect in postProcessEffects)
            effect.Apply(data);

        bitmap.UnlockBits(data);

        if(texts is not [])
        {
            using Graphics g = Graphics.FromImage(bitmap);
            foreach(TextElement t in texts)
                g.DrawString(t.text, t.font, Brushes.White, t.rect);
        }

        return bitmap;
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
        float dist = hit.vert ? sideDist.x - deltaDist.x : sideDist.y - deltaDist.y;
        if(camera.fixFisheyeEffect) 
            dist *= MathF.Cos(baseAngle);
        float dist01 = Utils.Clamp01(dist / camera.maxDist);

        if(dist > camera.maxDist || dist == 0f || depthBuf[x] < dist01)
            return;

        depthBuf[x] = dist01;

        float heightF = virtRes.y / dist / 2f;
        float brightness = (hit.vert ? 1f : .75f) * GetDistanceFogUnclamped(dist01);

        LockedBitmap tex = map.textures[(int)hit.tile];
        Vec2f rayDir = Vec2f.FromAngle(rayAngle);
        float wallX = (hit.vert ? camera.pos.y + dist * rayDir.y : camera.pos.x + dist * rayDir.x) % 1f;
        int texX = (int)(wallX * tex.data.Width);
        if(hit.vert && rayDir.x > 0f || !hit.vert && rayDir.y < 0f)
            texX = tex.data.Width - texX - 1;

        byte* texPtr = (byte*)tex.data.Scan0 + texX*3;

        int height = (int)heightF,
            y0 = Math.Max(0, virtualCenter.y - height),
            y1 = Math.Min(virtRes.y-1, virtualCenter.y + height),
            tMin = y0 - virtualCenter.y + height,
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


    private static float GetDistanceFogUnclamped(float dist01)
    {
        float inv = 1f - dist01;
        return .75f * inv*inv;
    }

    private static float GetDistanceFog(float dist01)
        => GetDistanceFogUnclamped(Utils.Clamp01(dist01));
}