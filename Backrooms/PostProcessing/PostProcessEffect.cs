using System;
using System.Drawing.Imaging;

namespace Backrooms.PostProcessing;

public abstract unsafe class PostProcessEffect(bool enabled = true)
{
    public bool enabled = enabled;


    public abstract bool requireRefBitmap { get; }


    /// <summary>Apply and draw effect to data</summary>
    public void Apply(BitmapData data)
    {
        if(!enabled)
            return;

        if(!requireRefBitmap)
        {
            Exec((byte*)data.Scan0, data.Stride, data.Width, data.Height);
            return;
        }

        int numBytes = data.Stride * data.Height;
        byte* refScan0 = stackalloc byte[numBytes];
        Buffer.MemoryCopy((void*)data.Scan0, refScan0, numBytes, numBytes);

        Exec((byte*)data.Scan0, refScan0, data.Stride, data.Width, data.Height);
    }


    /// <summary>Implemented if requireRefBitmap == false, scan0 is both the reference and modified bitmap</summary>
    protected abstract void Exec(byte* scan0, int stride, int w, int h);
    /// <summary>Implemented if requireRefBitmap == true, scan0 is the bitmap to be modified, refScan0 is the reference bitmap</summary>
    protected abstract void Exec(byte* scan0, byte* refScan0, int stride, int w, int h);

    protected void ThrowWrongExecExc<T>() where T : PostProcessEffect
    {
        if(requireRefBitmap)
            throw new($"The {typeof(T).Name} PostProcessEffect requires a reference bitmap!");
        else
            throw new($"The {typeof(T).Name} PostProcessEffect does not require a reference bitmap!");
    }
}