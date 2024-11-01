using System;

namespace Backrooms.Gui;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class GuiElementAttribute() : Attribute()
{
    public bool isSafe { get; init; }
    public bool isUnsafe { get; init; }
}