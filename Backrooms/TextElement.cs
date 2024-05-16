using System.Drawing;

namespace Backrooms;

public class TextElement(string text, RectangleF rect, FontFamily fontFamily, float sizePx)
{
    public string text = text;
    public RectangleF rect = rect;
    public Font font = new(fontFamily, sizePx, GraphicsUnit.Pixel);
}