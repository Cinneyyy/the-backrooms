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

    private Input input;


    public override bool isUnsafe => false;
    public override bool isSafe => false;


    private void Tick(float dt)
    {
        if(!enabled)
            return;

        bool isHovering = input.ContainsNormCursorCentered(location, size * group.sizeRatioFactor);

        if(isHovering)
        {
            if(input.MbDown(button))
                onClick?.Invoke();

            backgroundElem.color = input.MbHelt(button) ? colors.select : colors.hover;
        }
        else
            backgroundElem.color = colors.normal;
    }


    public override void OnAddedToGroup()
    {
        input = rend.input;
        group.groupEnabledTick += Tick;

        group.Add(textElem);
        group.Add(backgroundElem);
    }

    public override void OnRemovedFromGroup()
    {
        group.groupEnabledTick -= Tick;

        group.Remove(textElem);
        group.Remove(backgroundElem);
    }


    protected override void OnToggle()
    {
        textElem.enabled = enabled;
        backgroundElem.enabled = enabled;
    }
}