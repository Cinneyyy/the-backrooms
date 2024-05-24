﻿using System.Drawing;

namespace Backrooms.Gui;

public class TextElement(string text, FontFamily fontFamily, float emSize, Color color, StringFormat format, Vec2f location, Vec2f size, Anchor anchor = Anchor.C) : GuiElement(location, size, anchor)
{
    private const GraphicsUnit GRAPHICS_UNIT = GraphicsUnit.Pixel;

    public string text = text;
    public StringFormat format = format;

    private Brush brush = new SolidBrush(color);
    private Color _color = color;
    private float _emSize = emSize;
    private FontFamily _fontFamily = fontFamily;
    private Font font = new(fontFamily, emSize, GRAPHICS_UNIT);

    public static readonly StringFormat defaultFormat = new() {
        Alignment = StringAlignment.Near,
        LineAlignment = StringAlignment.Near,
        Trimming = StringTrimming.None
    };


    public override bool isUnsafe => false;
    public Color color
    {
        get => _color;
        set {
            _color = value;
            brush = new SolidBrush(_color);
        }
    }
    public float emSize
    {
        get => _emSize;
        set {
            _emSize = value;
            font = new(fontFamily, value, GRAPHICS_UNIT);
        }
    }
    public FontFamily fontFamily
    {
        get => _fontFamily;
        set {
            _fontFamily = value;
            font = new(value, emSize, GRAPHICS_UNIT);
        }
    }


    public TextElement(string text, FontFamily fontFamily, float emSize, Color color, Vec2f location, Vec2f size, Anchor anchor = Anchor.C) : this(text, fontFamily, emSize, color, defaultFormat, location, size, anchor) { }


    public override void DrawSafe(Graphics g)
        => g.DrawString(text, font, brush, new RectangleF(screenLocationF.x, screenLocationF.y, screenSizeF.x, screenSizeF.y), format);

    public override unsafe void DrawUnsafe(byte* scan, int stride, int w, int h) => throw new System.NotImplementedException();
}