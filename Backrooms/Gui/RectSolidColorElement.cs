using System;
using System.Drawing;

namespace Backrooms.Gui;

public class RectSolidColorElement(string name, Color color, Vec2f location, Vec2f size, Anchor anchor = Anchor.C) : GuiElement(name, location, size, anchor)
{
    public Color color = color;


    public override bool isUnsafe => true;
    public override bool isSafe => false;


    public override unsafe void DrawUnsafe(byte* scan, int stride, int w, int h)
    {
        scan += screenLocation.y * stride + screenLocation.x * 3;
        float alpha = color.A/255f;
        float r = Utils.Sqr(color.R/255f), g = Utils.Sqr(color.G/255f), b = Utils.Sqr(color.B/255f);

        for(int i = 0; i < screenSize.y; i++)
        {
            for(int j = 0; j < screenSize.x; j++)
            {
                int o = 3*j;
                byte* pixel = scan + o;

                *pixel = (byte)(MathF.Sqrt(Utils.Lerp(Utils.Sqr(*pixel++/255f), b, alpha)) * 255f);
                *pixel = (byte)(MathF.Sqrt(Utils.Lerp(Utils.Sqr(*pixel++/255f), g, alpha)) * 255f);
                *pixel = (byte)(MathF.Sqrt(Utils.Lerp(Utils.Sqr(*pixel/255f), r, alpha)) * 255f);
            }

            scan += stride;
        }
    }

    public override void DrawSafe(Graphics g) => throw new System.NotImplementedException();
}