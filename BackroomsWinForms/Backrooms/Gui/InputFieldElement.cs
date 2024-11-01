using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Backrooms.Gui;

[GuiElement()]
public class InputFieldElement(string name, FontFamily font, float textSize, Color textColor, ColorBlock colors, Vec2f loc, Vec2f size, Vec2f? anchor = null) : GuiElement(name, loc, size, anchor)
{
    public readonly TextElement textElem = new($"{name}_text", "", font, textSize, textColor, Vec2f.half, loc, size, anchor);
    public readonly RectSolidColorElement bgElem = new($"{name}_bg", colors.normal, true, loc, size, anchor);
    public event Action select, deselect;
    public event Action<string> valueChanged;

    private Input input;
    private string _text;

    public static readonly Keys[] typableKeys = [
        ..Enumerable.Range((int)Keys.A, (int)Keys.Z).Select(k => (Keys)k),
        ..Enumerable.Range((int)Keys.D0, (int)Keys.D9).Select(k => (Keys)k),
        Keys.OemPeriod, Keys.Space];


    public string text
    {
        get => _text;
        set {
            _text = value;
            valueChanged?.Invoke(value);
        }
    }
    public bool selected { get; private set; }

    public static bool someTextFieldSelected { get; private set; }


    public override void OnAddedToGroup()
    {
        group.groupEnabledTick += Tick;
        input = group.input;

        group.Add(bgElem);
        group.Add(textElem);
    }

    public override void OnRemovedFromGroup()
    {
        group.groupEnabledTick -= Tick;
        input = null;

        group.Remove(bgElem);
        group.Remove(textElem);
    }


    protected override void OnToggle()
    {
        textElem.enabled = enabled;
        bgElem.enabled = enabled;
    }


    private void Tick(float dt)
    {
        if(!enabled)
            return;

        bool isHovering = input.ContainsNormCursorCentered(location, size * group.guiToVirtRatio);

        if(selected && input.anyKeyDown)
        {
            char? newChar = null;

            foreach(Keys key in typableKeys)
                if(input.KeyDown(key))
                    newChar = (key | (input.KeyHelt(Keys.LShiftKey) ? Keys.Shift : 0)).ToChar();

            if(newChar is not null)
                text += newChar;

            if(input.KeyDown(Keys.Back))
                text = text[..^2];
        }

        if(selected && input.KeyDown(Keys.Return))
        {
            selected = false;
            deselect?.Invoke();
        }

        if(isHovering)
        {
            if(group.mbDown)
            {
                select?.Invoke();
                selected = true;
            }

            bgElem.color = group.mbHelt ? colors.select : colors.hover;
        }
        else
        {
            if(selected && group.mbDown)
            {
                someTextFieldSelected = false;
                deselect?.Invoke();
            }

            bgElem.color = colors.normal;
        }
    }
}