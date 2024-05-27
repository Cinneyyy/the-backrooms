using System.Drawing;

namespace Backrooms.Gui;

public class TextElement(string name, string text, FontFamily fontFamily, float emSize, Color color, StringFormat format, Anchor textAnchor, Vec2f location, Vec2f size, Anchor anchor = Anchor.C) : GuiElement(name, location, size, anchor)
{
    private const GraphicsUnit GRAPHICS_UNIT = GraphicsUnit.Millimeter;

    public string text = text;
    public StringFormat format = format;
    public Anchor textAnchor = textAnchor;

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


    public TextElement(string name, string text, FontFamily fontFamily, float emSize, Color color, Anchor textAnchor, Vec2f location, Vec2f size, Anchor anchor = Anchor.C) : this(name, text, fontFamily, emSize, color, defaultFormat, textAnchor, location, size, anchor) { }


    public override void DrawSafe(Graphics g)
    {
        RectangleF drawRect = new(screenLocationF.x, screenLocationF.y, screenSizeF.x, screenSizeF.y);
        SizeF measured = g.MeasureString(text, font, drawRect.Size, format);

        PointF loc = new(drawRect.X + (textAnchor & Anchor.HMask) switch {
                Anchor.Left => drawRect.Width - measured.Width,
                Anchor.Right => 0f,
                Anchor.Center => drawRect.Width/2 - measured.Width/2f,
                _ => throw new($"Invalid anchor ;; {textAnchor} ;; {(int)textAnchor}")
            }, drawRect.Y + (textAnchor & Anchor.VMask) switch{
                Anchor.Top => drawRect.Height - measured.Height,
                Anchor.Bottom => 0f,
                Anchor.Center => drawRect.Height/2f - measured.Height/2f,
                _ => throw new($"Invalid anchor ;; {textAnchor} ;; {(int)textAnchor}")
            });

        g.DrawString(text, font, brush, drawRect with { Location = loc }, format);
    }

    public override unsafe void DrawUnsafe(byte* scan, int stride, int w, int h) => throw new System.NotImplementedException();


    protected override void ScreenDimensionsChanged() 
    {
        font.Dispose();
        font = new(fontFamily, emSize * rend.downscaleFactor, GRAPHICS_UNIT);
    }
}