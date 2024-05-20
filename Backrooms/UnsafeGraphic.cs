using System.Drawing;
using System.Drawing.Imaging;

namespace Backrooms;

public unsafe class UnsafeGraphic
{
    public readonly BitmapData data;
    public readonly bool useAlpha;
    public readonly byte* scan0;
    public readonly int stride, w, h;

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
    }

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
    }


    public void Unlock()
        => bitmap.UnlockBits(data);
    public void Unlock(Bitmap bitmap)
        => bitmap.UnlockBits(data);


    /// <summary>Only use for setting a single pixel, as it calculates the buffer offset each call!</summary>
    public void SetPixel(int x, int y, byte r, byte g, byte b)
    {
        byte* scan = scan0 + y*stride + x*3;
        *scan = b;
        *++scan = g;
        *++scan = r;
    }
    /// <summary>Only use for setting a single pixel, as it calculates the buffer offset each call!</summary>
    public void SetPixel(int x, int y, byte r, byte g, byte b, byte a)
    {
        byte* scan = scan0 + y*stride + x*4;
        *scan = b;
        *++scan = g;
        *++scan = r;
        *++scan = a;
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
}