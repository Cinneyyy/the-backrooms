using System;

namespace Backrooms.PostProcessing;

public unsafe class VDistortion(Func<float, float> distort, Color32? emptyCol = null, bool enabled = true) : PostProcessEffect(enabled)
{
    public Func<float, float> distort = distort;
    public Color32 emptyCol = emptyCol ?? Color32.black;


    public override bool requireRefBitmap => false;


    protected override void Exec(byte* scan0, int stride, int w, int h)
    {
        for(int x = 0; x < w; x++)
        {
            int offset = (int)(distort(x / (w-1f)) * h);
            int pixOffset = -3 * offset;
            int absOffset = Math.Abs(offset);

            if(offset > 0)
            {

            }
            else if(offset < 0)
            {

            }
        }
    }
    protected override unsafe void Exec(byte* scan0, byte* refScan0, int stride, int w, int h)
        => ThrowWrongExecExc<HDistortion>();
}