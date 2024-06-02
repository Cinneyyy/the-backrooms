using System;
using System.Drawing;
using System.Windows.Forms;

namespace Backrooms.Gui;

public class CheckboxElement(string name, string text, FontFamily font, float textSize, Color color, ColorBlock colors, UnsafeGraphic checkmark, float checkmarkSize, bool isOn, Action<bool> valueChanged, Vec2f location, Vec2f size, Anchor anchor = Anchor.C) : GuiElement(name, location, size, anchor)
{
    public readonly TextElement textElem = new($"{name}_text", text, font, textSize, color, Anchor.Left, location, size, anchor);
    public readonly RectSolidColorElement backgroundElem = new($"{name}_background", colors.normal, new(), new(size.y), Anchor.C);
    public readonly ImageElement checkmarkElem = new($"{name}_checkmark", checkmark, color, new(), new(size.y * checkmarkSize), Anchor.C) {
        enabled = isOn
    };
    public MouseButtons button = MouseButtons.Left;

    private Input input;
    private bool _isOn = isOn;


    public override bool isSafe => false;
    public override bool isUnsafe => false;
    public bool isOn
    {
        get => _isOn;
        set {
            _isOn = value;
            checkmarkElem.enabled = value;
            valueChanged?.Invoke(value);
        }
    }


    public CheckboxElement(string name, string text, FontFamily font, float textSize, Color textColor, ColorBlock colors, string checkmark, float checkmarkSize, bool isOn, Action<bool> valueChanged, Vec2f location, Vec2f size, Anchor anchor = Anchor.C) : this(name, text, font, textSize, textColor, colors, new UnsafeGraphic(checkmark), checkmarkSize, isOn, valueChanged, location, size, anchor) { }


    public override void OnAddedToGroup()
    {
        input = rend.input;
        group.groupEnabledTick += Tick;

        group.Add(textElem);
        group.Add(backgroundElem);
        group.Add(checkmarkElem);

        backgroundElem.location = checkmarkElem.location = location + new Vec2f((size.x - size.y) / 2f, 0f);
    }

    public override void OnRemovedFromGroup()
    {
        group.groupEnabledTick -= Tick;

        group.Remove(textElem);
        group.Remove(backgroundElem);
        group.Remove(checkmarkElem);
    }


    protected override void OnToggle()
    {
        textElem.enabled = enabled;
        checkmarkElem.enabled = enabled;
        backgroundElem.enabled = enabled;
    }


    private void Tick(float dt)
    {
        if(!enabled)
            return;

        bool isHovering = input.ContainsCursorCentered(backgroundElem.screenLocation + backgroundElem.screenSize/2, backgroundElem.screenSize);

        if(isHovering)
            isOn ^= input.MbDown(button);

        backgroundElem.color = !isHovering ? colors.normal : (input.MbHelt(button) ? colors.select : colors.hover);
    }
}