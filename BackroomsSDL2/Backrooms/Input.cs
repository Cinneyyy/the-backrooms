using System;
using System.Collections.Generic;
using System.Linq;

namespace Backrooms;

public static class Input
{
    internal static class Internal
    {
        private static readonly HashSet<Key> additionPending = [], removalPending = [];


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

        internal static void MouseMove(Vec2i abs, Vec2i rel)
        {
            rawMousePos = abs;
            rawMouseDelta = rel;

            mousePos = Vec2f.Clamp(
                (abs - Renderer.outputOffset) * Renderer.downscale / Renderer.res,
                Vec2f.zero, Vec2f.one);

            if(relativeMouse)
                mouseDelta = rel * Renderer.downscale / Renderer.res;
        }

        internal static void TickPrePolling()
            => mouseDelta = Vec2f.zero;

        internal static void TickPostPolling()
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


    private static readonly HashSet<Key> keyState = [];
    private static HashSet<Key> lastKeyState = [];


    public static Vec2f mousePos { get; private set; }
    public static Vec2f mouseDelta { get; private set; }
    public static Vec2i rawMousePos { get; private set; }
    public static Vec2i rawMouseDelta { get; private set; }
    public static bool anyKey => keyState.Count > 0;
    public static bool anyKeyDown => !keyState.Any(lastKeyState.Contains);
    public static bool anyKeyUp => !lastKeyState.Any(keyState.Contains);

    private static bool _relativeMouse;
    public static bool relativeMouse
    {
        get => _relativeMouse;
        set
        {
            _relativeMouse = value;
            if(SDL_SetRelativeMouseMode(value ? SDL_bool.SDL_TRUE : SDL_bool.SDL_FALSE) < 0)
                throw new($"Failed to set cursor: {SDL_GetError()}");
        }
    }


    public static bool KeyHelt(Key key) => keyState.Contains(key);
    public static bool KeyDown(Key key) => keyState.Contains(key) && !lastKeyState.Contains(key);
    public static bool KeyUp(Key key) => !keyState.Contains(key) && lastKeyState.Contains(key);
}