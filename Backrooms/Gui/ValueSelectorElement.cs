﻿using System;
using System.Drawing;

namespace Backrooms.Gui;

[GuiElement]
public class ValueSelectorElement : GuiElement
{
    public readonly ImageElement leftArrowElem, rightArrowElem;
    public readonly RectSolidColorElement leftBackgroundElem, rightBackgroundElem;
    public readonly TextElement textElem;
    public readonly string[] values;
    public ColorBlock colors;
    public event Action<int> valueChanged;
    public event Action<string> valueChangedStr;
    public OverflowBehaviour overflowBehaviour = OverflowBehaviour.Mod;

    private Input input;
    private int _value;


    public int value
    {
        get => _value;
        set {
            if(value < 0 || value >= values.Length)
                value = overflowBehaviour switch {
                    OverflowBehaviour.None => value,
                    OverflowBehaviour.Clamp => Utils.Clamp(value, 0, values.Length-1),
                    OverflowBehaviour.Mod => Utils.Mod(value, values.Length),
                    _ => throw new($"Invalid OverflowBehaviour: {overflowBehaviour} ({(int)overflowBehaviour})")
                };

            _value = value;

            textElem.text = values[value];
            valueChanged?.Invoke(value);
            valueChangedStr?.Invoke(values[value]);
        }
    }
    public string strValue
    {
        get => values[value];
        set => this.value = Array.IndexOf(values, value);
    }
    public Image upArrowSprite
    {
        set {
            Bitmap clockwise = new(value);
            Bitmap counter = new(value);
            clockwise.RotateFlip(RotateFlipType.Rotate90FlipNone);
            counter.RotateFlip(RotateFlipType.Rotate90FlipX);
            rightArrowElem.graphic = new(clockwise, true);
            leftArrowElem.graphic = new(counter, true);
        }
    }


    public ValueSelectorElement(string name, string[] values, int startValue, Color textColor, FontFamily font, float textSize, ColorBlock colors, bool fastBlend, Image upArrow, float arrowSize, Action<int> valueChanged, Vec2f location, Vec2f size, Vec2f? anchor = null) : base(name, location, size, anchor)
    {
        this.values = values;
        _value = startValue;
        this.colors = colors;
        this.valueChanged = valueChanged;

        textElem = new($"{name}_text", values.Length == 0 ? string.Empty : values[startValue], font, textSize, textColor, Vec2f.half, location, size);
        leftArrowElem = new($"{name}_leftarrow", graphic: null, textColor, new(), new(size.y * arrowSize));
        rightArrowElem = new($"{name}_rightarrow", graphic: null, textColor, new(), new(size.y * arrowSize));
        leftBackgroundElem = new($"{name}_leftbg", colors.normal, fastBlend, new(), new(size.y));
        rightBackgroundElem = new($"{name}_rightbg", colors.normal, fastBlend, new(), new(size.y));

        upArrowSprite = upArrow;
    }

    public ValueSelectorElement(string name, string[] values, int startValue, Color textColor, FontFamily font, float textSize, ColorBlock colors, bool fastBlend, string upArrowSprite, float arrowSize, Action<int> valueChanged, Vec2f location, Vec2f size, Vec2f? anchor = null) : this(name, values, startValue, textColor, font, textSize, colors, fastBlend, Resources.sprites[upArrowSprite], arrowSize, valueChanged, location, size, anchor) { }


    public override void OnAddedToGroup()
    {
        group.groupEnabledTick += Tick;
        input = group.input;

        group.AddMany([leftBackgroundElem, rightBackgroundElem, leftArrowElem, rightArrowElem, textElem]);

        rightArrowElem.location = rightBackgroundElem.location = location + new Vec2f((size.x - size.y) / 2f, 0f);
        leftArrowElem.location = leftBackgroundElem.location = new(1f - rightArrowElem.location.x, rightArrowElem.location.y);
    }

    public override void OnRemovedFromGroup()
    {
        group.groupEnabledTick -= Tick;
        input = null;

        group.RemoveMany([leftBackgroundElem, rightBackgroundElem, leftArrowElem, rightArrowElem, textElem]);
    }

    public void SetWithoutNotify(int value)
    {
        if(value < 0 || value >= values.Length)
            value = overflowBehaviour switch {
                OverflowBehaviour.None => value,
                OverflowBehaviour.Clamp => Utils.Clamp(value, 0, values.Length-1),
                OverflowBehaviour.Mod => Utils.Mod(value, values.Length),
                _ => throw new($"Invalid OverflowBehaviour: {overflowBehaviour} ({(int)overflowBehaviour})")
            };

        _value = value;
        textElem.text = values[value];
    }


    protected override void OnToggle()
    {
        textElem.enabled = enabled;
        leftArrowElem.enabled = enabled;
        rightArrowElem.enabled = enabled;
        leftBackgroundElem.enabled = enabled;
        rightBackgroundElem.enabled = enabled;
    }


    private void Tick(float dt)
    {
        if(!enabled)
            return;

        bool lHover = input.ContainsCursorCentered(leftBackgroundElem.screenLocation + leftBackgroundElem.screenSize/2, leftBackgroundElem.screenSize);
        bool rHover = input.ContainsCursorCentered(rightBackgroundElem.screenLocation + rightBackgroundElem.screenSize/2, rightBackgroundElem.screenSize);

        bool mbDown = group.mbDown;

        if(lHover && mbDown)
            value--;
        if(rHover && mbDown)
            value++;

        leftBackgroundElem.color = !lHover ? colors.normal : (mbDown ? colors.select : colors.hover);
        rightBackgroundElem.color = !rHover ? colors.normal : (mbDown ? colors.select : colors.hover);
    }
}