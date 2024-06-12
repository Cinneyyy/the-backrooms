namespace Backrooms.Gui;

public enum ElementSafety
{
    Neither = 0,
    Safe = 1,
    Unsafe = 2,
    Both = Safe | Unsafe
}