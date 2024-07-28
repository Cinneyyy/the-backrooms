using System;
using System.Drawing;

namespace Backrooms.Gui;

[GuiElement(safety = ElementSafety.Unsafe)]
public class RectSolidColorElement(string name, Color color, bool fastBlend, Vec2f location, Vec2f size, Vec2f? anchor = null) : GuiElement(name, location, size, anchor)
{
    public Color color = color;
    public bool fastBlend = fastBlend;


    public override unsafe void DrawUnsafe(byte* scan, int stride, int w, int h)
    {
        scan += Math.Max(screenLocation.y, 0) * stride + Math.Max(screenLocation.x, 0) * 3;

        int maxY = Math.Min(screenSize.y, rend.virtRes.y - screenLocation.y);
        int maxX = Math.Min(screenSize.x, rend.virtRes.x - screenLocation.x);

        float alpha = color.A/255f;
        float r = color.R/255f, g = color.G/255f, b = color.B/255f;

        for(int i = 0; i < maxY; i++)
        {
            for(int j = 0; j < maxX; j++)
            {
                int o = 3*j;
                byte* pixel = scan + o;

                if(fastBlend)
                {
                    *pixel = (byte)Utils.Lerp(*pixel++, color.B, alpha);
                    *pixel = (byte)Utils.Lerp(*pixel++, color.G, alpha);
                    *pixel = (byte)Utils.Lerp(*pixel, color.R, alpha);
                }
                else
                {
                    *pixel = (byte)(MathF.Sqrt(Utils.Lerp(Utils.Sqr(*pixel++/255f), b*b, alpha)) * 255f);
                    *pixel = (byte)(MathF.Sqrt(Utils.Lerp(Utils.Sqr(*pixel++/255f), g*g, alpha)) * 255f);
                    *pixel = (byte)(MathF.Sqrt(Utils.Lerp(Utils.Sqr(*pixel/255f), r*r, alpha)) * 255f);
                }
            }

            scan += stride;
        }
    }

    public override void DrawSafe(Graphics g) => throw new System.NotImplementedException();
}