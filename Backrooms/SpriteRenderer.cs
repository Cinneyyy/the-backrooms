using System.Drawing;

namespace Backrooms;

public class SpriteRenderer(Vec2f pos, Vec2f size, Image image)
{
    public UnsafeGraphic graphic = new(image, true);
    public Image image = image;
    public Vec2f pos = pos, size = size;


    public void SetImage(Image image)
        => graphic = new(image, true);
}