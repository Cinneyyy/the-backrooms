using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Backrooms;

public unsafe class LockedBitmap : IDisposable
{
    private readonly Bitmap bitmap;
    private readonly PixelFormat pixelFormat;
    private BitmapData data;


    public LockedBitmap(Image src, PixelFormat pixelFormat, bool preLock = true)
    {
        bitmap = new(src);
        this.pixelFormat = pixelFormat;
        if(preLock)
            Lock();
    }


    void IDisposable.Dispose()
    {
        bitmap.UnlockBits(data);
        bitmap.Dispose();
        GC.SuppressFinalize(this);
    }


    public void Unlock()
        => bitmap.UnlockBits(data);

    public void Lock()
        => data = bitmap.LockBits(new(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, pixelFormat);

    public void SetPixel24(int x, int y, Color32 col)
    {
        byte* ptr = (byte*)data.Scan0 + data.Stride * y + 3 * x;
        *(ptr++) = col.b;
        *(ptr++) = col.g;
        *ptr = col.r;
    }
    public void SetPixel32(int x, int y, Color32 col)
    {
        byte* ptr = (byte*)data.Scan0 + data.Stride * y + 4 * x;
        *(ptr++) = col.b;
        *(ptr++) = col.g;
        *(ptr++) = col.r;
        *ptr = col.a;
    }

    public Color32 GetPixel24(int x, int y)
    {
        byte* ptr = (byte*)data.Scan0 + data.Stride * y + 3 * x;
        Color32 col = new() {
            b = *ptr++,
            g = *ptr++,
            r = *ptr
        };
        return col;
    }
    public Color32 GetPixel32(int x, int y)
    {
        byte* ptr = (byte*)data.Scan0 + data.Stride * y + 4 * x;
        Color32 col = new() {
            b = *ptr++,
            g = *ptr++,
            r = *ptr++,
            a = *ptr
        };
        return col;
    }

    public Color32 GetUv24(float u, float v)
        => GetPixel24((int)(u * bitmap.Width), (int)(v * bitmap.Height));
    public Color32 GetUv32(float u, float v)
        => GetPixel32((int)(u * bitmap.Width), (int)(v * bitmap.Height));

    public BitmapData GetBitmapData()
        => data;
}
