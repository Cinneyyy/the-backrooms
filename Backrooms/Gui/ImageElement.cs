using System.Drawing;

namespace Backrooms.Gui;

public class ImageElement(string name, UnsafeGraphic image, Vec2f location, Vec2f size, Anchor anchor = Anchor.C) : GuiElement(name, location, size, anchor)
{
    public UnsafeGraphic image = image;
    public float rMul = 1f, gMul = 1f, bMul = 1f;


    public override bool isUnsafe => true;
    public override bool isSafe => false;


    public ImageElement(string name, string spriteId, Vec2f location, Vec2f size, Anchor anchor = Anchor.C) : this(name, new UnsafeGraphic(Resources.sprites[spriteId], true), location, size, anchor) { }


    public override unsafe void DrawUnsafe(byte* scan, int stride, int w, int h)
    {
        Assert(image.useAlpha, "ImageElement.image has to use 32-bit format!");

        scan += screenLocation.y * stride + screenLocation.x * 3;

        for(int i = 0; i < screenSize.y; i++)
        {
            for(int j = 0; j < screenSize.x; j++)
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

    public override void DrawSafe(Graphics g) => throw new System.NotImplementedException();
}