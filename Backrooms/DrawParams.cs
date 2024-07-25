using System;

namespace Backrooms;

[Flags]
public enum DrawParams : byte
{
    Columns = 1,
    Sprites = 2,
    Gui = 4,
    PostEffects = 8,
    FloorAndCeil = 16,
    All = 255
}