﻿using System;

namespace Backrooms.PostProcessing;

public unsafe class VDistortion(Func<float, float> distort, Color32? emptyCol = null, bool enabled = true) : PostProcessEffect(enabled)
{
    public Func<float, float> distort = distort;
    public Color32 emptyCol = emptyCol ?? Color32.black;


    public override bool requireRefBitmap { get; } = false;


    protected override void Exec(byte* scan0, int stride, int w, int h)
    {
        for(int x = 0; x < w; x++)
        {
            int offset = (distort(x / (w-1f)) * h).Round();
            int pixOffset = -stride * offset;
            int absOffset = Math.Abs(offset);
            byte* scan = scan0 + 3 * x;

            if(offset > 0)
            {
                scan += stride * h - stride;

                for(int y = absOffset; y < h; y++)
                {
                    *scan = *(scan + pixOffset);
                    *(scan+1) = *(scan+1 + pixOffset);
                    *(scan+2) = *(scan+2 + pixOffset);

                    scan -= stride;
                }

                for(int y = 0; y < absOffset; y++)
                {
                    *scan = emptyCol.b;
                    *(scan+1) = emptyCol.g;
                    *(scan+2) = emptyCol.r;

                    scan -= stride;
                }
            }
            else if(offset < 0)
            {
                int offsetHeight = h-absOffset;
                
                for(int y = 0; y < offsetHeight; y++)
                {
                    *scan = *(scan + pixOffset);
                    *(scan+1) = *(scan+1 + pixOffset);
                    *(scan+2) = *(scan+2 + pixOffset);

                    scan += stride;
                }

                for(int y = offsetHeight; y < h; y++)
                {
                    *scan = emptyCol.b;
                    *(scan+1) = emptyCol.g;
                    *(scan+2) = emptyCol.r;

                    scan += stride;
                }
            }
        }
    }
    protected override unsafe void Exec(byte* scan0, byte* refScan0, int stride, int w, int h)
        => ThrowWrongExecExc<HDistortion>();
}