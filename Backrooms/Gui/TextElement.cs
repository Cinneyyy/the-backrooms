using System.Drawing;

namespace Backrooms.Gui;

public class TextElement(string name, string text, FontFamily fontFamily, float emSize, Color color, StringFormat format, Vec2f location, Vec2f size, Anchor anchor = Anchor.C) : GuiElement(name, location, size, anchor)
{
    private const GraphicsUnit GRAPHICS_UNIT = GraphicsUnit.Millimeter;

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
            brush.Dispose();
            brush = new SolidBrush(_color);
        }
    }
    public float emSize
    {
        get => _emSize;
        set {
            _emSize = value;
            font.Dispose();
            font = new(fontFamily, value / group.rend.downscaleFactor, GRAPHICS_UNIT);
        }
    }
    public FontFamily fontFamily
    {
        get => _fontFamily;
        set {
            _fontFamily = value;
            font.Dispose();
            font = new(value, emSize * group.rend.downscaleFactor, GRAPHICS_UNIT);
        }
    }


    public TextElement(string name, string text, FontFamily fontFamily, float emSize, Color color, Vec2f location, Vec2f size, Anchor anchor = Anchor.C) : this(name, text, fontFamily, emSize, color, defaultFormat, location, size, anchor) { }


    public override void DrawSafe(Graphics g)
        => g.DrawString(text, font, brush, new RectangleF(screenLocationF.x, screenLocationF.y, screenSizeF.x, screenSizeF.y), format);

    public override unsafe void DrawUnsafe(byte* scan, int stride, int w, int h) => throw new System.NotImplementedException();


    protected override void ScreenDimensionsChanged() 
    {
        font.Dispose();
        font = new(fontFamily, emSize * rend.downscaleFactor, GRAPHICS_UNIT);
    }
}