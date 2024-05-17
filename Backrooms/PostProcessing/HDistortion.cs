using System;

namespace Backrooms.PostProcessing;

public unsafe class HDistortion(Func<float, float> distort, Color32? emptyCol = null, bool enabled = true) : PostProcessEffect(enabled)
{
    public Func<float, float> distort = distort;
    public Color32 emptyCol = emptyCol ?? Color32.black;


    public override bool requireRefBitmap => false;


    protected override void Exec(byte* scan0, int stride, int w, int h)
    {
        for(int y = 0; y < h; y++)
        {
            int offset = (int)(distort(y / (h-1f)) * w);
            int pixOffset = -3 * offset;
            int absOffset = Math.Abs(offset);
            int x;

            if(offset > 0)
            {
                scan0 += stride;

                for(x = absOffset; x < w; x++)
                {
                    *--scan0 = *(scan0 + pixOffset);
                    *--scan0 = *(scan0 + pixOffset);
                    *--scan0 = *(scan0 + pixOffset);
                }

                for(x = 0; x < absOffset; x++)
                {
                    *--scan0 = emptyCol.r;
                    *--scan0 = emptyCol.g;
                    *--scan0 = emptyCol.b;
                }

                scan0 += stride;
            }
            else if(offset < 0)
            {
                int offsetWidth = w-absOffset;

                for(x = 0; x < offsetWidth; x++)
                {
                    *scan0 = *(scan0++ + pixOffset);
                    *scan0 = *(scan0++ + pixOffset);
                    *scan0 = *(scan0++ + pixOffset);
                }

                for(x = offsetWidth; x < w; x++)
                {
                    *scan0++ = emptyCol.b;
                    *scan0++ = emptyCol.g;
                    *scan0++ = emptyCol.r;
                }
            }
            else
                scan0 += stride;
        }
    }
    protected override unsafe void Exec(byte* scan0, byte* refScan0, int stride, int w, int h)
        => ThrowWrongExecExc<HDistortion>();
}