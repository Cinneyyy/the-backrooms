using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Backrooms;

public unsafe class UnsafeGraphic : IDisposable
{
    public readonly BitmapData data;
    public readonly bool useAlpha;
    public readonly byte* scan0;
    public readonly int stride, w, h, wb, hb;
    public readonly Vec2i size, bounds;

    private readonly Bitmap bitmap;


    public UnsafeGraphic(Bitmap bitmap, bool useAlpha)
    {
        this.bitmap = bitmap;
        data = bitmap.LockBits(new(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, useAlpha ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb);
        this.useAlpha = useAlpha;
        scan0 = (byte*)data.Scan0;
        stride = data.Stride;
        w = data.Width;
        h = data.Height;
        wb = w-1;
        hb = h-1;
        size = new(w, h);
        bounds = new(wb, hb);
    }

    public UnsafeGraphic(Image img, bool useAlpha) : this(new(img), useAlpha) { }

    public UnsafeGraphic(BitmapData data)
    {
        Assert(data.PixelFormat is PixelFormat.Format24bppRgb or PixelFormat.Format32bppArgb, "Pixel format of bitmap data has to be either 24-bit RGB or 32-bit ARGB!");

        bitmap = null;
        this.data = data;
        useAlpha = data.PixelFormat == PixelFormat.Format32bppArgb;
        scan0 = (byte*)data.Scan0;
        stride = data.Stride;
        w = data.Width;
        h = data.Width;
        wb = w-1;
        hb = h-1;
        size = new(w, h);
        bounds = new(wb, hb);
    }


    /// <summary>Only call if the reference bitmap, if even passed in, is not used after this Dispose() call</summary>
    public void Dispose()
    {
        if(bitmap is not null)
        {
            Unlock();
            bitmap.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    public void Unlock()
        => bitmap.UnlockBits(data);
    public void Unlock(Bitmap bitmap)
        => bitmap.UnlockBits(data);


    #region Image operations
    public void SetPixel(int x, int y, byte r, byte g, byte b)
    {
        byte* scan = scan0 + y*stride + x*3;
        *scan = b;
        *++scan = g;
        *++scan = r;
    }
    public void SetPixel(int x, int y, byte r, byte g, byte b, byte a)
    {
        byte* scan = scan0 + y*stride + x*4;
        *scan = b;
        *++scan = g;
        *++scan = r;
        *++scan = a;
    }

    public (byte r, byte g, byte b) GetPixelRgb(int x, int y)
    {
        byte* scan = scan0 + y*stride + x*3;
        return (*(scan+2), *(scan+1), *scan);
    }
    public (byte r, byte g, byte b, byte a) GetPixelArgb(int x, int y)
    {
        byte* scan = scan0 + y*stride + x*4;
        return (*(scan+2), *(scan+1), *scan, *(scan+3));
    }

    public void FillRow(int x0, int x1, int y, byte r, byte g, byte b)
    {
        byte* scan = scan0 + y*stride + x0*3;

        for(int x = x0; x < x1; x++)
        {
            *scan++ = b;
            *scan++ = g;
            *scan++ = r;
        }
    }
    public void FillRow(int x0, int x1, int y, byte r, byte g, byte b, byte a)
    {
        byte* scan = scan0 + y*stride + x0*4;

        for(int x = x0; x < x1; x++)
        {
            *scan++ = b;
            *scan++ = g;
            *scan++ = r;
            *scan++ = a;
        }
    }

    public void FillColumn(int x, int y0, int y1, byte r, byte g, byte b)
    {
        byte* scan = scan0 + y0*stride + x*3;

        for(int y = y0; y < y1; x++)
        {
            *scan = b;
            *(scan+1) = g;
            *(scan+2) = r;

            scan += stride;
        }
    }
    public void FillColumn(int x, int y0, int y1, byte r, byte g, byte b, byte a)
    {
        byte* scan = scan0 + y0*stride + x*4;

        for(int y = y0; y < y1; x++)
        {
            *scan = b;
            *(scan+1) = g;
            *(scan+2) = r;
            *(scan+3) = a;

            scan += stride;
        }
    }
    #endregion
}