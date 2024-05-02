#define DONT_CLEAR
#undef DONT_CLEAR
#define FIX_FISHEYE

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Backrooms;

public unsafe class Renderer
{
    public readonly Vec2i virtualRes, physicalRes;
    public readonly Vec2i virtualCenter, physicalCenter;
    public readonly Vec2i outputRes, outputLocation;
    public readonly float downscaleFactor, upscaleFactor;
    public Camera camera;
    public Input input;
    public Map map;
    public Window window;
    public List<SpriteRenderer> sprites = [];
    public float[] depthBuf;
    public bool drawIfCursorOffscreen = true;

#if DONT_CLEAR
    private Bitmap lastBmp = new(1, 1);
#endif


    public Renderer(Vec2i virtualRes, Vec2i physicalRes, Window window)
    {
        this.virtualRes = virtualRes;
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

#if DONT_CLEAR
        Bitmap bitmap = new(lastBmp, virtualRes.x, virtualRes.y);
#else
        Bitmap bitmap = new(virtualRes.x, virtualRes.y);
#endif

        Array.Fill(depthBuf, 1f);
        BitmapData data = bitmap.LockBits(new(0, 0, virtualRes.x, virtualRes.y), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

        int sprCount = sprites.Count;
        sprites.Sort((a, b) => (int)MathF.Round((b.pos - camera.pos).sqrLength - (a.pos - camera.pos).sqrLength));
        for(int i = 0; i < sprCount; i++)
        {
            SpriteRenderer spr = sprites[i];
            Vec2f relPos = spr.pos - camera.pos;

            float dist = relPos.length;
            if(dist == 0f)
                continue;

            relPos.Normalize();
            Vec2f size = virtualRes.y / dist * spr.size;
            float relDot = Vec2f.Dot(Vec2f.FromAngle(camera.angle + MathF.PI/2f).normalized, relPos);

            if(Vec2f.Dot(camera.forward.normalized, relPos) >= 0f && size.x > 0f && size.y > 0f)
            {
                // idk why the /1.5f nearly fixes the sprite moving too much with the camera angle, but somehow it does
                int locX = (int)(relDot/1.5f * virtualRes.x + virtualCenter.x - size.x/2f);
                int locY = (int)(virtualCenter.y - size.y/2f);
                Vec2i sizeI = size.Round();

                int xMin = Math.Max(0, locX), xMax = Math.Min(virtualRes.x-1, locX+sizeI.x);
                for(int x = xMin; x < xMax; x++) // Draw wall behind transparent image
                    DrawWallSegment(data, in x);

                if(spr.hasTransparency)
                    DrawBitmapCutout24(data, spr.lockedImage.data, locX, locY, sizeI.x, sizeI.y, GetDistanceFog(dist));
                else 
                    DrawBitmap24(data, spr.lockedImage.data, locX, locY, sizeI.x, sizeI.y, GetDistanceFog(dist));

                FillDepthBuf(locX, sizeI.x, dist/camera.maxDist);
            }
        }

        for(int x = 0; x < virtualRes.x; x++)
            DrawWallSegment(data, in x);

        bitmap.UnlockBits(data);
#if DONT_CLEAR
        lastBmp = bitmap;
#endif
        return bitmap;
    }


    private void DrawWallSegment(BitmapData data, in int x)
    {
        float baseAngle = camera.fov * (x / (virtualRes.x-1f) - .5f);
        float rayAngle = Utils.NormAngle(camera.angle + baseAngle);

        if(!Raycast(map, camera.pos, Vec2f.FromAngle(rayAngle), out Vec2i hit) || (camera.pos - (Vec2f)hit).length > camera.maxDist)
            return;

        GetIntersectionData(camera.pos, hit, rayAngle, out Side side, out float interTime, out float dist);
        Tile tile = map[hit.x, hit.y];

        if(dist > camera.maxDist || dist == 0f || dist/camera.maxDist is float dist01 && GetDepthBuf(x) < dist01)
            return;

        SetDepthBuf(x, dist01);

#if FIX_FISHEYE
        float fisheyeDist = dist * MathF.Cos(baseAngle);
#else
        float fisheyeDist = dist;
#endif

        float height = MathF.Max(0f, virtualRes.y / fisheyeDist / 2f);
        float max = 2f * height;
        float brightness = MathF.Min(1f, (int)side/5f + .5f) * GetDistanceFog(dist);

        int biggestY = Math.Min(virtualRes.y-1, (int)MathF.Ceiling(virtualCenter.y + height));
        for(int y = Math.Max(virtualCenter.y - (int)height, 0), i = y - virtualCenter.y + (int)height; y < biggestY; y++, i++)
            SetPixel24(data, x, y, map.textures[(int)tile].GetUv24(interTime, i/max) * brightness);
    }

    private float GetDistanceFog(float dist)
        => MathF.Max(0f, MathF.Pow(.8f - dist / camera.maxDist, 1.5f));

    private void SetDepthBuf(int x, float depth)
        => depthBuf[x] = depth;

    private float GetDepthBuf(int x)
        => depthBuf[x];

    private void FillDepthBuf(int x, int w, float depth)
    {
        int fx = Math.Min(virtualRes.x, x+w);
        for(int i = Math.Max(0, x); i < fx; i++)
            depthBuf[i] = depth;
    }

    private unsafe void SetPixel24(BitmapData data, int x, int y, Color32 col)
    {
        byte* ptr = (byte*)data.Scan0 + data.Stride * y + 3 * x;
        *ptr++ = col.b;
        *ptr++ = col.g;
        *ptr = col.r;
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

    private static bool Raycast(Map map, Vec2f from, Vec2f dir, out Vec2i hitLoc)
    {
        Vec2i initMap = Map.Round(from);
        Vec2f deltaDist = new(
            dir.x == 0f ? float.MaxValue : MathF.Abs(1f / dir.x),
            dir.y == 0f ? float.MaxValue : MathF.Abs(1f / dir.y));

        Vec2i step = new();
        Vec2f initSideDist = new();
        (step.x, initSideDist.x) = dir.x < 0f
            ? (-1, (from.x - initMap.x) * deltaDist.x)
            : (1, (initMap.x + 1f - from.x) * deltaDist.x);
        (step.y, initSideDist.y) = dir.y < 0f
            ? (-1, (from.y - initMap.y) * deltaDist.y)
            : (1, (initMap.y + 1f - from.y) * deltaDist.y);

        (bool hit, Vec2i pt, Vec2f sideDist, bool northOrSouth) res = (false, initMap, initSideDist, initSideDist.x < initSideDist.y);
        while(!res.hit && res.pt.x < map.size.x && res.pt.y < map.size.y)
        {
            Vec2i inMap;
            Vec2f sideDist;
            bool northOrSouth;

            if(res.sideDist.x < res.sideDist.y)
            {
                inMap = new(res.pt.x + step.x, res.pt.y);
                northOrSouth = true;
                sideDist = new(res.sideDist.x + deltaDist.x, res.sideDist.y);
            }
            else
            {
                inMap = new(res.pt.x, res.pt.y + step.y);
                northOrSouth = false;
                sideDist = new(res.sideDist.x, res.sideDist.y + deltaDist.y);
            }

            bool inTile = map.InBounds(inMap) && map[inMap.x, inMap.y] != 0;
            res = (inTile, inMap, sideDist, northOrSouth);
        }

        hitLoc = res.pt;
        //northOrSouth = res.northOrSouth
        return res.hit;
    }

    private static void GetIntersectionData(Vec2f player, Vec2i tile, float angle, out Side interSide, out float interTime, out float dist)
    {
        Vec2f center = new(tile.x + .5f, tile.y + .5f);

        // Corners to player
        Vec2f tl = new Vec2f(tile.x,    tile.y) - player,
              tr = new Vec2f(tile.x+1f, tile.y) - player,
              bl = new Vec2f(tile.x,    tile.y+1f) - player,
              br = new Vec2f(tile.x+1f, tile.y+1f) - player;

        Dir playerToTile;
        if(Utils.RoughlyEqual(player.y, center.y, 0.5f)) // On the same y as tile, so it has to be W or E
            playerToTile = player.x > center.x ? Dir.East : Dir.West;
        else if(Utils.RoughlyEqual(player.x, center.x, 0.5f)) // On the same x as tile, so it has to be N or S
            playerToTile = player.y > center.y ? Dir.South : Dir.North;
        else if(player.x > center.x) // To right of tile
            playerToTile = player.y > center.y ? Dir.SE : Dir.NE;
        else if(player.x < center.x) // To the left of tile
            playerToTile = player.y > center.y ? Dir.SW : Dir.NW;
        else
            throw new($"Could not identifiy where player {player} was in relation to tile {tile}");

        // Angles from player to corner
        float tlA = tl.toAngle,
              trA = tr.toAngle,
              blA = bl.toAngle,
              brA = br.toAngle;

        interSide = playerToTile switch {
            Dir.North => Side.North,
            Dir.South => Side.South,
            Dir.West => Side.West,
            Dir.East => Side.East,

            Dir.NE => angle < trA ? Side.East : Side.North,
            Dir.NW => angle < tlA ? Side.North : Side.West,
            Dir.SW => angle < blA ? Side.West : Side.South,
            Dir.SE => angle < brA ? Side.South : Side.East,

            _ => throw new($"Could not identifiy the intersection side of player {player}, intersecting tile {tile}")
        };

        // Distances from corners to player
        float tlL = tl.length,
              trL = tr.length,
              blL = bl.length,
              brL = br.length;

        (float minA, float maxA, float minL, float maxL) = interSide switch {
            Side.East => (brA, trA, brL, trL),
            Side.West => (tlA, blA, tlL, blL),
            Side.North => (trA, tlA, trL, tlL),
            Side.South => (blA, brA, blL, brL),
            _ => throw new()
        };

        void offset_angles(float amount)
            => (minA, maxA, angle) = ((minA + amount) % MathF.Tau, (maxA + amount) % MathF.Tau, (angle + amount) % MathF.Tau);

        //int dy = (int)MathF.Floor(player.y); // forgot why i did this might change l8r
        //if(interSide == Side.West &&
        //    (angle > MathF.PI && player.y < tile.y
        //    || angle < MathF.PI && player.y > tile.y
        //    || angle < MathF.PI && dy == tile.y))
        //    if(dy == tile.y)
        //        offset_angles(MathF.PI);
        //    else
        //        offset_angles(MathF.PI);
        //else if(interSide == Side.North && angle > MathF.PI || interSide == Side.South && angle < MathF.PI)
        //    offset_angles(MathF.PI);
        if(interSide == Side.West || interSide == Side.North && angle > MathF.PI || interSide == Side.South && angle < MathF.PI)
            offset_angles(MathF.PI);

        interTime = (angle - minA) / (maxA - minA);
        dist = Utils.LerpUnclamped(minL, maxL, interTime);
    }
}