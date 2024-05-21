using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Backrooms;

public class SpriteRenderer(Vec2f pos, Vec2f size, bool hasTransparency, Image image)
{
    public UnsafeGraphic lockedImage = new(image, hasTransparency);
    public Image image = image;
    public Vec2f pos = pos, size = size;
    public bool hasTransparency = hasTransparency;


    public void SetImage(Image image, bool hasTransparency)
        => lockedImage = new(image, hasTransparency);
}