using System;

namespace Backrooms.Gui;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class GuiElementAttribute() : Attribute
{
    public bool isSafe { get; init; } = false;
    public bool isUnsafe { get; init; } = false;
    public ElementSafety safety
    {
        get => (isSafe ? ElementSafety.Safe : ElementSafety.Neither) | (isUnsafe ? ElementSafety.Unsafe : ElementSafety.Neither);
        init {
            isSafe = (value & ElementSafety.Safe) != 0;
            isUnsafe = (value & ElementSafety.Unsafe) != 0;
        }
    }
}