using System.Drawing;

namespace Backrooms;

public class TextElement(string text, RectangleF rect, FontFamily fontFamily, float sizePx, bool enabled = true)
{
    public bool enabled = enabled;
    public string text = text;
    public RectangleF rect = rect;
    public Font font = new(fontFamily, sizePx, GraphicsUnit.Pixel);
}