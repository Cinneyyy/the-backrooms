using System;

namespace Backrooms.PostProcessing;

public unsafe class HVDistortion(Func<float, float> distortX, Func<float, float> distortY, Color32? emptyCol = null, bool enabled = true) : PostProcessEffect(enabled)
{
    private readonly HDistortion hDistortion = new(distortX, emptyCol, true);
    private readonly VDistortion vDistortion = new(distortY, emptyCol, true);


    public override bool requireRefBitmap => false;
    public Func<float, float> distortX
    {
        get => hDistortion.distort;
        set => hDistortion.distort = value;
    }
    public Func<float, float> distortY
    {
        get => vDistortion.distort;
        set => vDistortion.distort = value;
    }
    public Func<float, float> distort
    {
        get => distortX != distortY ? throw new("H- & VDistortion have different distort functions!") : distortX;
        set => distortX = distortY = value;
    }
    public Color32 emptyCol
    {
        get => hDistortion.emptyCol;
        set => hDistortion.emptyCol = vDistortion.emptyCol = value;
    }


    public HVDistortion(Func<float, float> distort, Color32? emptyCol = null, bool enabled = true) : this(distort, distort, emptyCol, enabled) { }


    protected override void Exec(byte* scan0, int stride, int w, int h)
    {
        hDistortion.ApplyUnsafe(scan0, stride, w, h);
        vDistortion.ApplyUnsafe(scan0, stride, w, h);
    }
    protected override unsafe void Exec(byte* scan0, byte* refScan0, int stride, int w, int h)
        => ThrowWrongExecExc<HDistortion>();
}