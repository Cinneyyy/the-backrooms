using System;
using System.Drawing;

namespace Backrooms.Gui;

[GuiElement(safety = ElementSafety.Neither)]
public class CheckboxElement(string name, string text, FontFamily font, float textSize, Color color, ColorBlock colors, bool fastBlend, UnsafeGraphic checkmark, float checkmarkSize, bool isOn, Action<bool> valueChanged, Vec2f location, Vec2f size, Vec2f? anchor = null) : GuiElement(name, location, size, anchor)
{
    public readonly TextElement textElem = new($"{name}_text", text, font, textSize, color, new(0f, .5f), location, size, anchor);
    public readonly RectSolidColorElement backgroundElem = new($"{name}_background", colors.normal, fastBlend, new(), new(size.y), null);
    public readonly ImageElement checkmarkElem = new($"{name}_checkmark", checkmark, color, new(), new(size.y * checkmarkSize), null) {
        enabled = isOn
    };

    private Input input;
    private bool _isOn = isOn;


    public bool isOn
    {
        get => _isOn;
        set {
            if(_isOn == value)
                return;

            _isOn = value;
            checkmarkElem.enabled = value;
            valueChanged?.Invoke(value);
        }
    }


    public CheckboxElement(string name, string text, FontFamily font, float textSize, Color textColor, ColorBlock colors, bool fastBlend, string checkmark, float checkmarkSize, bool isOn, Action<bool> valueChanged, Vec2f location, Vec2f size, Vec2f? anchor = null) : this(name, text, font, textSize, textColor, colors, fastBlend, new UnsafeGraphic(checkmark), checkmarkSize, isOn, valueChanged, location, size, anchor) { }


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
            isOn ^= group.mbDown;

        backgroundElem.color = !isHovering ? colors.normal : (group.mbHelt ? colors.select : colors.hover);
    }
}