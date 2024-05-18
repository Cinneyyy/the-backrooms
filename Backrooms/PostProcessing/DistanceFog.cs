using System;

namespace Backrooms.PostProcessing;

public class DistanceFog(Func<float, float> calcFog, float[] depthBuf, bool enabled = true) : PostProcessEffect(enabled)
{
    public override bool requireRefBitmap => false;


    protected override unsafe void Exec(byte* scan0, int stride, int w, int h)
    {
        for(int x = 0; x < w; x++)
        {
            float fog = calcFog(depthBuf[x]);
            byte* scan = scan0 + 3*x;

            for(int y = 0; y < h; y++)
            {
                *scan = (byte)(*scan * fog);
                *(scan+1) = (byte)(*(scan+1) * fog);
                *(scan+2) = (byte)(*(scan+2) * fog);

                scan += stride;
            }
        }
    }
    protected override unsafe void Exec(byte* scan0, byte* refScan0, int stride, int w, int h)
        => ThrowWrongExecExc<DistanceFog>();
}