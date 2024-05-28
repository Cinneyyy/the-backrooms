using System;
using System.Drawing;
using System.Windows.Forms;

namespace Backrooms.Gui;

public class ButtonElement(string name, string text, FontFamily font, float fontSize, Color textColor, ColorBlock colors, Action onClick, Vec2f location, Vec2f size, Anchor anchor = Anchor.C) : GuiElement(name, location, size, anchor)
{

    public readonly TextElement textElem = new($"{name}_text", text, font, fontSize, textColor, Anchor.Center, location, size, anchor);
    public readonly RectSolidColorElement backgroundElem = new($"{name}_background", colors.normal, location, size, anchor);
    public ColorBlock colors = colors;
    public MouseButtons button = MouseButtons.Left;
    public event Action onClick = onClick;

    private Vec2f sizeCollFactor;
    private Input input;


    public override bool isUnsafe => true;
    public override bool isSafe => true;


    public override void DrawSafe(Graphics g) => textElem.DrawSafe(g);
    public override unsafe void DrawUnsafe(byte* scan, int stride, int w, int h) => backgroundElem.DrawUnsafe(scan, stride, w, h);


    private void Tick(float dt)
    {
        if(!enabled || !group.enabled)
            return;

        bool isHovering = Utils.InsideRect(location, size * sizeCollFactor, input.normMousePos);

        if(isHovering)
        {
            if(input.MbDown(button))
            {
                Out($"Invoked button '{name}' (From group '{group.name}')");
                onClick?.Invoke();
            }

            backgroundElem.color = input.MbHelt(button) ? colors.select : colors.hover;
        }
        else
            backgroundElem.color = colors.normal;
    }


    public override void OnAddedToGroup()
    {
        rend.window.tick += Tick;
        input = rend.input;
        textElem.group = group;
        backgroundElem.group = group;
    }


    protected override void ScreenDimensionsChanged()
    {
        sizeCollFactor = new Vec2f(1f / rend.virtRatio, 1f);
        textElem.ReloadScreenDimensions();
        backgroundElem.ReloadScreenDimensions();
    }
}