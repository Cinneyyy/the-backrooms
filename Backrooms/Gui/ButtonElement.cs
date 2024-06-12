﻿using System;
using System.Drawing;

namespace Backrooms.Gui;

[GuiElement(safety = ElementSafety.Neither)]
public class ButtonElement(string name, string text, FontFamily font, float fontSize, Color textColor, ColorBlock colors, Action onClick, Vec2f location, Vec2f size, Anchor anchor = Anchor.C, bool hasText = true) : GuiElement(name, location, size, anchor)
{
    public readonly TextElement textElem = !hasText ? null : new($"{name}_text", text, font, fontSize, textColor, Anchor.Center, location, size, anchor);
    public readonly RectSolidColorElement backgroundElem = new($"{name}_background", colors.normal, location, size, anchor);
    public readonly bool hasText = hasText;
    public ColorBlock colors = colors;
    public event Action onClick = onClick;

    private Input input;


    public ButtonElement(string name, ColorBlock colors, Action onClick, Vec2f location, Vec2f size, Anchor anchor = Anchor.C) : this(name, null, null, 0f, default, colors, onClick, location, size, anchor, false) { }


    private void Tick(float dt)
    {
        if(!enabled)
            return;

        bool isHovering = input.ContainsNormCursorCentered(location, size * group.sizeRatioFactor);

        if(isHovering)
        {
            if(group.mbDown)
                onClick?.Invoke();

            backgroundElem.color = group.mbHelt ? colors.select : colors.hover;
        }
        else
            backgroundElem.color = colors.normal;
    }


    public override void OnAddedToGroup()
    {
        input = rend.input;
        group.groupEnabledTick += Tick;

        if(hasText)
            group.Add(textElem);
        group.Add(backgroundElem);
    }

    public override void OnRemovedFromGroup()
    {
        group.groupEnabledTick -= Tick;

        if(hasText)
            group.Remove(textElem);
        group.Remove(backgroundElem);
    }


    protected override void OnToggle()
    {
        if(hasText)
            textElem.enabled = enabled;
        backgroundElem.enabled = enabled;
    }
}