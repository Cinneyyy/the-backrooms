using System;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Backrooms;

public class PictureBoxWithDrawOptions : PictureBox
{
    public InterpolationMode InterpolationMode { get; set; }
    public SmoothingMode SmoothingMode { get; set; }
    public PixelOffsetMode PixelOffsetMode { get; set; }
    public CompositingQuality CompositingQuality { get; set; }


    protected override void OnPaint(PaintEventArgs paintEventArgs)
    {
        try
        {
            paintEventArgs.Graphics.InterpolationMode = InterpolationMode;
            paintEventArgs.Graphics.SmoothingMode = SmoothingMode;
            paintEventArgs.Graphics.PixelOffsetMode = PixelOffsetMode;
            paintEventArgs.Graphics.CompositingQuality = CompositingQuality;

            base.OnPaint(paintEventArgs);
        }
        catch(Exception exc)
        {
            Out(exc, ConsoleColor.Red);
        }
    }
}