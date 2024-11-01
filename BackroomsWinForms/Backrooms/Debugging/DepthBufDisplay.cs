using Backrooms.PostProcessing;

namespace Backrooms.Debugging;

public class DepthBufDisplay(Renderer rend) : PostProcessEffect(false)
{
    public readonly Renderer rend = rend;
    public int height = 16;


    public override bool requireRefBitmap => false;


    protected override unsafe void Exec(byte* scan0, int stride, int w, int h)
    {
        for(int y = 0; y < height; y++)
        {
            byte* scan = scan0 + y * stride;

            for(int x = 0; x < w; x++)
            {
                byte val = (byte)(255 * Utils.Clamp01(1f - rend.depthBuf[x]));
                *scan0++ = val;
                *scan0++ = val;
                *scan0++ = val;
            }
        }
    }
    protected override unsafe void Exec(byte* scan0, byte* refScan0, int stride, int w, int h) => ThrowWrongExecExc<DepthBufDisplay>();
}