﻿using System;
using System.Drawing;

namespace Backrooms.Gui;

[GuiElement()]
public class ButtonElement(string name, string text, FontFamily font, float fontSize, Color textColor, ColorBlock colors, bool fastBlend, Action onClick, Vec2f location, Vec2f size, Vec2f? anchor = null, bool hasText = true)
    : GuiElement(name, location, size, anchor)
{
    public readonly TextElement textElem = !hasText ? null : new($"{name}_text", text, font, fontSize, textColor, null, Vec2f.half, location, size, anchor ?? Vec2f.half);
    public readonly RectSolidColorElement backgroundElem = new($"{name}_background", colors.normal, fastBlend, location, size, anchor);
    public readonly bool hasText = hasText;
    public ColorBlock colors = colors;
    public event Action onClick = onClick;

    private Input input;


    public ButtonElement(string name, ColorBlock colors, bool fastBlend, Action onClick, Vec2f location, Vec2f size, Vec2f? anchor = null) : this(name, null, null, 0f, default, colors, fastBlend, onClick, location, size, anchor, false) { }


    public override void OnAddedToGroup()
    {
        group.groupEnabledTick += Tick;
        input = group.input;

        group.Add(backgroundElem);
        if(hasText)
            group.Add(textElem);
    }

    public override void OnRemovedFromGroup()
    {
        group.groupEnabledTick -= Tick;
        input = null;

        group.Remove(backgroundElem);
        if(hasText)
            group.Remove(textElem);
    }


    protected override void OnToggle()
    {
        if(hasText)
            textElem.enabled = enabled;
        backgroundElem.enabled = enabled;
    }


    private void Tick(float dt)
    {
        if(!enabled)
            return;

        bool isHovering = input.ContainsNormCursorCentered(location, size * group.guiToVirtRatio);

        if(isHovering)
        {
            if(group.mbDown)
                onClick?.Invoke();

            backgroundElem.color = group.mbHelt ? colors.select : colors.hover;
        }
        else
            backgroundElem.color = colors.normal;
    }
}