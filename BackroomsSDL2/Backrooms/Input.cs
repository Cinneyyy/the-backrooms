using System;
using System.Collections.Generic;
using System.Linq;

namespace Backrooms;

public static class Input
{
    internal static class Internal
    {
        internal static void KeyDown(int keycode)
        {
            Key key = (Key)keycode;

            if(Enum.IsDefined(key) && key != Key.None && !keyState.Contains(key))
                additionPending.Add(key);
        }

        internal static void KeyUp(int keycode)
        {
            Key key = (Key)keycode;

            if(Enum.IsDefined(key) && key != Key.None && keyState.Contains(key))
                removalPending.Add(key);
        }

        internal static void Tick()
        {
            lastKeyState = new(keyState);

            foreach(Key key in removalPending)
                keyState.Remove(key);
            foreach(Key key in additionPending)
                keyState.Add(key);

            removalPending.Clear();
            additionPending.Clear();
        }
    }


    private static readonly HashSet<Key> additionPending = [], removalPending = [], keyState = [];
    private static HashSet<Key> lastKeyState = [];


    public static bool anyKey => keyState.Count > 0;
    public static bool anyKeyDown => !keyState.Any(lastKeyState.Contains);
    public static bool anyKeyUp => !lastKeyState.Any(keyState.Contains);


    public static bool KeyHelt(Key key) => keyState.Contains(key);
    public static bool KeyDown(Key key) => keyState.Contains(key) && !lastKeyState.Contains(key);
    public static bool KeyUp(Key key) => !keyState.Contains(key) && lastKeyState.Contains(key);
}