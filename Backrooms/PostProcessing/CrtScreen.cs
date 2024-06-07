using System;

namespace Backrooms.PostProcessing;

public class CrtScreen(Vec2f? distortionConstants = null, CrtScreen.CrtLensFunction size = null, bool enabled = true) : PostProcessEffect(enabled)
{
    public delegate float CrtLensFunction(float t, float distortionConst);


    public CrtLensFunction size = size ?? ((t, c) => (-Utils.Sqr(t) + t + c - .25f) / c);
    public Vec2f distortionConstants = distortionConstants ?? new(3.15f, 2.75f);


    public override bool requireRefBitmap { get; } = true;


    protected override unsafe void Exec(byte* scan0, byte* refScan0, int stride, int w, int h)
    {
        for(int x = 0; x < w; x++)
        {
            float height = size(x/(w-1f), distortionConstants.y);
            
            int heightPx = (height * h).Floor();
            int y0 = ((h - heightPx)/2f).Floor(), y1 = ((h + heightPx)/2f).Floor();

            if(y0 == 0)
                continue;

            byte* scan = scan0 + x*3;
            int dStride = stride - 2;

            for(int y = 0; y < y0; y++)
            {
                *scan++ = 0;
                *scan++ = 0;
                *scan = 0;

                scan += dStride;
            }

            for(int y = y0, i = 0; y < y1; y++, i++)
            {
                byte* refScan = refScan0 + i*h/heightPx*stride + x*3;
                *scan++ = *refScan++;
                *scan++ = *refScan++;
                *scan = *refScan;

                scan += dStride;
            }

            for(int y = y1; y < h; y++)
            {
                *scan++ = 0;
                *scan++ = 0;
                *scan = 0;

                scan += dStride;
            }
        }

        byte* row = stackalloc byte[w*3];
        for(int y = 0; y < h; y++)
        {
            float width = size(y/(h-1f), distortionConstants.x);

            int widthPx = (width * w).Floor();
            int x0 = ((w - widthPx)/2f).Floor(), x1 = ((w + widthPx)/2f).Floor();

            if(x0 == 0)
                continue;

            byte* scan = scan0 + y*stride;
            Buffer.MemoryCopy(scan, row, w*3, w*3);
            
            for(int x = 0; x < 3*x0; x++)
                *scan++ = 0;

            for(int x = x0, i = 0; x < x1; x++, i++)
            {
                byte* pix = row + i*w/widthPx*3;
                *scan++ = *pix++;
                *scan++ = *pix++;
                *scan++ = *pix;
            }

            int fillRight = 3*(w-x1);
            for(int x = 0; x < fillRight; x++)
                *scan++ = 0;
        }
    }
    protected override unsafe void Exec(byte* scan0, int stride, int w, int h)
        => ThrowWrongExecExc<CrtScreen>();
}