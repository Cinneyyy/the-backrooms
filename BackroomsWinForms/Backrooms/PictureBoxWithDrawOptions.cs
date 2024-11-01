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
            // Intentional formatting abuse ("{exc.Message}" instead of "$e"), because this issue is just console-spam otherwise
            OutErr(Log.Info, exc, $"{exc.GetType()} in OnPaint (PictureBoxWithDrawOptions.cs) ;; {exc.Message}");
        }
    }
}