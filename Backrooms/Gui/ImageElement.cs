﻿using System;
using System.Drawing;

namespace Backrooms.Gui;

[GuiElement(safety = ElementSafety.Unsafe)]
public class ImageElement(string name, UnsafeGraphic image, Color color, Vec2f location, Vec2f size, Anchor anchor = Anchor.C) : GuiElement(name, location, size, anchor)
{
    public UnsafeGraphic image = image;
    public float rMul = color.R/255f, gMul = color.G/255f, bMul = color.B/255f;


    public ImageElement(string name, string spriteId, Color color, Vec2f location, Vec2f size, Anchor anchor = Anchor.C) : this(name, new UnsafeGraphic(Resources.sprites[spriteId], true), color, location, size, anchor) { }


    public override unsafe void DrawUnsafe(byte* scan, int stride, int w, int h)
    {
        Assert(image.useAlpha, "ImageElement.image has to use 32-bit format!");

        scan += Math.Max(screenLocation.y, 0) * stride + Math.Max(screenLocation.x, 0) * 3;

        int maxY = Math.Min(screenSize.y, rend.virtRes.y - screenLocation.y);
        int maxX = Math.Min(screenSize.x, rend.virtRes.x - screenLocation.x);

        for(int i = 0; i < maxY; i++)
        {
            for(int j = 0; j < maxX; j++)
            {
                (byte r, byte g, byte b, byte a) color = image.GetUvRgba(j / (screenSizeF.x-1f), i / (screenSizeF.y-1f));

                if(color.a > 0x80)
                {
                    int o = 3*j;
                    *(scan + o) = (byte)(color.b * bMul);
                    *(scan+1 + o) = (byte)(color.g * gMul);
                    *(scan+2 + o) = (byte)(color.r * rMul);
                }
            }

            scan += stride;
        }
    }
}