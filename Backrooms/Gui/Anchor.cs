using System;

namespace Backrooms.Gui;

public enum Anchor : byte
{
    Center = 0,
    Top = 1,
    Bottom = 2,
    Left = 4,
    Right = 8,
    TopLeft = Top | Left,
    TopRight = Top | Right,
    BottomLeft = Bottom | Left,
    BottomRight = Bottom | Right,

    C = Center,
    T = Top,
    B = Bottom,
    L = Left,
    R = Right,
    TL = TopLeft,
    TR = TopRight,
    BL = BottomLeft,
    BR = BottomRight,

    VMask = Top | Bottom,
    HMask = Left | Right,
}