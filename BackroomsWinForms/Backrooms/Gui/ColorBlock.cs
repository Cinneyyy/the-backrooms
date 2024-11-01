using System.Drawing;

namespace Backrooms.Gui;

public record class ColorBlock(Color normal, Color hover, Color select)
{
    public ColorBlock(Color baseColor, byte normalAlpha, byte hoverAlpha, byte selectAlpha) : this(
        Color.FromArgb(normalAlpha, baseColor),
        Color.FromArgb(hoverAlpha, baseColor),
        Color.FromArgb(selectAlpha, baseColor)) { }

    public ColorBlock(Color baseColor, float normalAlpha, float hoverAlpha, float selectAlpha) 
        : this(baseColor, (byte)(normalAlpha * 255f), (byte)(hoverAlpha * 255f), (byte)(selectAlpha * 255f)) { }

    public ColorBlock(Color baseColor, float normalValue, float hoverValue, float selectValue, bool changeValue, bool changeAlpha) : this(baseColor, baseColor, baseColor)
    {
        if(changeValue)
        {
            normal = Color.FromArgb(normal.A, (byte)(normal.R * normalValue), (byte)(normal.G * normalValue), (byte)(normal.B * normalValue));
            hover = Color.FromArgb(hover.A, (byte)(hover.R * hoverValue), (byte)(hover.G * hoverValue), (byte)(hover.B * hoverValue));
            select = Color.FromArgb(select.A, (byte)(select.R * selectValue), (byte)(select.G * selectValue), (byte)(select.B * selectValue));
        }

        if(changeAlpha)
        {
            normal = Color.FromArgb((byte)(normal.A * normalValue), normal);
            hover = Color.FromArgb((byte)(hover.A * hoverValue), hover);
            select = Color.FromArgb((byte)(select.A * selectValue), select);
        }
    }


    public Color GetColor(bool isHovering, bool isClicking)
        => !isHovering ? normal : (isClicking ? select : hover);
}