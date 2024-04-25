using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Backrooms;

public class SpriteRenderer(Vec2f pos, Vec2f size, Image image)
{
    public LockedBitmap lockedImage = new(image, PixelFormat.Format24bppRgb);
    public Image image = image;
    public Vec2f pos = pos, size = size;
}