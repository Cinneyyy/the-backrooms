using System;
using System.Drawing;
using System.Linq;

namespace Backrooms;

public class SpriteRenderer(Vec2f pos, Vec2f size, UnsafeGraphic[] graphics) : IDisposable
{
    public UnsafeGraphic[] graphics = graphics;
    public Vec2f pos = pos, size = size;
    public bool enabled = true;
    /// <summary>[-0.5;0.5], where 0 is the screen center</summary>
    public float elevation = 0f;
    public int importance = 0;
    public bool hasRotation = graphics.Length > 1;
    public float rot;


    public SpriteRenderer(Vec2f pos, Vec2f size, Image[] images) : this(pos, size, images.Select(i => new UnsafeGraphic(i, true)).ToArray()) { }

    public SpriteRenderer(Vec2f pos, Vec2f size, UnsafeGraphic graphic) : this(pos, size, [graphic]) { }

    public SpriteRenderer(Vec2f pos, Vec2f size, Image image) : this(pos, size, [new UnsafeGraphic(image, true)]) { }


    public void Dispose()
    {
        foreach(UnsafeGraphic g in graphics)
            g.Dispose();
        GC.SuppressFinalize(this);
    }

    public void SetImage(Image image)
        => graphics = [new(image, true)];

    public void SetImages(Image[] images)
        => graphics = images.Select(i => new UnsafeGraphic(i, true)).ToArray();

    public UnsafeGraphic GetGraphic(Vec2f camPos)
    {
        int index = hasRotation ? ((1f - Utils.Mod(rot - (camPos-pos).toAngle, MathF.Tau) / MathF.Tau) * graphics.Length).Floor() : 0;
        return graphics[index];
    }


    public void Ground()
        => elevation = (size.y - 1f) / 2f;
}