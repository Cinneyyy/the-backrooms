using System.Drawing.Imaging;

namespace Backrooms;

public abstract unsafe class PostProcessEffect(bool enabled = true)
{
    public bool enabled = enabled;


    public abstract bool requireRefBitmap { get; }


    /// <summary>Apply and draw effect to data</summary>
    public void Apply(BitmapData data)
    {
        Assert(!requireRefBitmap, "This PostProcessEffect takes in two bitmaps!");

        if(enabled)
            Exec((byte*)data.Scan0, data.Stride, data.Width, data.Height);
    }
    /// <summary>Apply effect to src, but drawing it onto target</summary>
    public void Apply(BitmapData src, BitmapData target)
    {
        Assert(requireRefBitmap, "This PostProcessEffect only takes in one bitmap!");

        if(enabled)
        {
            Assert(src.Width == target.Width && src.Height == target.Height && src.Stride == target.Stride, "src and target need to be of the same size!");
            Assert(src.Scan0 != target.Scan0, "src and target must be two different bitmaps!");
            Assert(src.PixelFormat == PixelFormat.Format24bppRgb && target.PixelFormat == PixelFormat.Format24bppRgb, "src and targe must both have PixelFormat.Format24bppRgb!");

            Exec((byte*)src.Scan0, (byte*)target.Scan0, src.Stride, src.Width, src.Height);
        }
    }


    /// <summary>Implemented if requireRefBitmap == false</summary>
    protected abstract void Exec(byte* scan0, int stride, int w, int h);
    /// <summary>Implemented if requireRefBitmap == true</summary>
    protected abstract void Exec(byte* srcScan0, byte* targetScan0, int stride, int w, int h);
}