using System.Drawing;

namespace Backrooms.Gui;

[GuiElement(isSafe = true)]
public class TextElement(string name, string text, FontFamily fontFamily, float emSize, Color color, StringFormat format, Vec2f textAnchor, Vec2f location, Vec2f size, Vec2f? anchor = null) : GuiElement(name, location, size, anchor)
{
    private const GraphicsUnit GRAPHICS_UNIT = GraphicsUnit.Millimeter;

    public string text = text;
    public StringFormat format = format;
    public Vec2f textAnchor = textAnchor;

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
            font = new(fontFamily, value / group.rend.singleDownscaleFactor, GRAPHICS_UNIT);
        }
    }
    public FontFamily fontFamily
    {
        get => _fontFamily;
        set {
            _fontFamily = value;
            font.Dispose();
            font = new(value, emSize * group.rend.singleDownscaleFactor, GRAPHICS_UNIT);
        }
    }


    public TextElement(string name, string text, FontFamily fontFamily, float emSize, Color color, Vec2f textAnchor, Vec2f location, Vec2f size, Vec2f? anchor = null) : this(name, text, fontFamily, emSize, color, defaultFormat, textAnchor, location, size, anchor) { }


    public override void DrawSafe(Graphics g)
    {
        SizeF screenSize = (SizeF)screenSizeF;
        Vec2f measured = (Vec2f)g.MeasureString(text, font, screenSize, format);
        PointF loc = (PointF)(textAnchor * (screenSizeF - measured) + screenLocationF);

        g.DrawString(text, font, brush, new RectangleF(loc, screenSize), format);
    }


    protected override void ScreenDimensionsChanged()
    {
        font.Dispose();
        font = new(fontFamily, emSize * rend.singleDownscaleFactor, GRAPHICS_UNIT);
    }
}