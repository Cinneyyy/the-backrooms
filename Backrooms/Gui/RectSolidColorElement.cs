using System;
using System.Drawing;

namespace Backrooms.Gui;

public class RectSolidColorElement(string name, Color color, Vec2f location, Vec2f size, Anchor anchor = Anchor.C) : GuiElement(name, location, size, anchor)
{
    public Color color = color;
    public bool accurateColorBlending = true;


    public override bool isUnsafe => true;
    public override bool isSafe => false;


    public override unsafe void DrawUnsafe(byte* scan, int stride, int w, int h)
    {
        scan += Math.Max(screenLocation.y, 0) * stride + Math.Max(screenLocation.x, 0) * 3;

        int maxY = Math.Min(screenSize.y, rend.virtRes.y - screenLocation.y);
        int maxX = Math.Min(screenSize.x, rend.virtRes.x - screenLocation.x);

        float alpha = color.A/255f;
        float r = Utils.Sqr(color.R/255f), g = Utils.Sqr(color.G/255f), b = Utils.Sqr(color.B/255f);

        for(int i = 0; i < maxY; i++)
        {
            for(int j = 0; j < maxX; j++)
            {
                int o = 3*j;
                byte* pixel = scan + o;

                if(accurateColorBlending)
                {
                    *pixel = (byte)(MathF.Sqrt(Utils.Lerp(Utils.Sqr(*pixel++/255f), b, alpha)) * 255f);
                    *pixel = (byte)(MathF.Sqrt(Utils.Lerp(Utils.Sqr(*pixel++/255f), g, alpha)) * 255f);
                    *pixel = (byte)(MathF.Sqrt(Utils.Lerp(Utils.Sqr(*pixel/255f), r, alpha)) * 255f);
                }
                else
                {
                    *pixel = (byte)Utils.Lerp(*pixel++, color.B, alpha);
                    *pixel = (byte)Utils.Lerp(*pixel++, color.G, alpha);
                    *pixel = (byte)Utils.Lerp(*pixel, color.R, alpha);
                }
            }

            scan += stride;
        }
    }

    public override void DrawSafe(Graphics g) => throw new System.NotImplementedException();
}