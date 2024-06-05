using System.Drawing;

namespace Backrooms;

public class SpriteRenderer(Vec2f pos, Vec2f size, UnsafeGraphic graphic)
{
    public UnsafeGraphic graphic = graphic;
    public Vec2f pos = pos, size = size;


    public SpriteRenderer(Vec2f pos, Vec2f size, Image image) : this(pos, size, new UnsafeGraphic(image, true)) { }


    public void SetImage(Image image)
        => graphic = new(image, true);
}