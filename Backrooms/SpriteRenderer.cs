﻿using System;
using System.Drawing;

namespace Backrooms;

public class SpriteRenderer(Vec2f pos, Vec2f size, UnsafeGraphic graphic) : IDisposable
{
    public UnsafeGraphic graphic = graphic;
    public Vec2f pos = pos, size = size;
    public bool enabled = true;


    public SpriteRenderer(Vec2f pos, Vec2f size, Image image) : this(pos, size, new UnsafeGraphic(image, true)) { }


    public void Dispose()
    {
        graphic.Dispose();
        GC.SuppressFinalize(this);
    }

    public void SetImage(Image image)
        => graphic = new(image, true);
}