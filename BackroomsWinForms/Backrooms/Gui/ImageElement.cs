using System;
using System.Drawing;

namespace Backrooms.Gui;

[GuiElement(isUnsafe = true)]
public class ImageElement(string name, UnsafeGraphic graphic, Color color, Vec2f location, Vec2f size, Vec2f? anchor = null) : GuiElement(name, location, size, anchor)
{
    public UnsafeGraphic graphic = graphic;
    public float rMul = color.R/255f, gMul = color.G/255f, bMul = color.B/255f;
    public bool fastColorBlend = true;


    public float mul
    {
        set => rMul = gMul = bMul = value;
    }


    public ImageElement(string name, string spriteId, Color color, Vec2f location, Vec2f size, Vec2f? anchor = null) : this(name, new UnsafeGraphic(Resources.sprites[spriteId], true), color, location, size, anchor) { }


    public override unsafe void DrawUnsafe(byte* scan, int stride, int w, int h)
    {
        if(graphic is null)
            return;

        Assert(Log.Info, graphic.useAlpha, "ImageElement.image has to use 32-bit format!");

        scan += Math.Max(screenLocation.y, 0) * stride + Math.Max(screenLocation.x, 0) * 3;

        int maxY = Math.Min(screenSize.y, rend.virtRes.y - screenLocation.y);
        int maxX = Math.Min(screenSize.x, rend.virtRes.x - screenLocation.x);

        for(int i = 0; i < maxY; i++)
        {
            for(int j = 0; j < maxX; j++)
            {
                (byte r, byte g, byte b, byte a) color = graphic.GetUvRgba(j / (screenSizeF.x-1f), i / (screenSizeF.y-1f));

                byte* lscan = scan + 3*j;
                if(color.a == 0xff)
                {
                    *(lscan) = (byte)(color.b * bMul);
                    *(lscan+1) = (byte)(color.g * gMul);
                    *(lscan+2) = (byte)(color.r * rMul);
                }
                else if(color.a != 0)
                {
                    if(fastColorBlend)
                        (*(lscan+2), *(lscan+1), *lscan) =
                            Utils.BlendColorsCrude(
                                *(lscan+2), *(lscan+1), *lscan,
                                (byte)(color.r * rMul), (byte)(color.g * gMul), (byte)(color.b * bMul),
                                color.a/255f);
                    else
                        (*(lscan+2), *(lscan+1), *lscan) =
                            Utils.BlendColors(
                                *(lscan+2), *(lscan+1), *lscan,
                                (byte)(color.r * rMul), (byte)(color.g * gMul), (byte)(color.b * bMul),
                                color.a/255f);
                }
            }

            scan += stride;
        }
    }
}