﻿using System.Collections.Generic;
using System.Windows.Forms;

namespace Backrooms;

public class Input(Vec2i screenRes, Vec2i screenLoc, bool lockMouse = true)
{
    public bool lockCursor = lockMouse;

    private readonly HashSet<Keys> additionPending = [], removalPending = [], keyState = [], lastKeyState = [];
    private readonly Vec2i screenRes = screenRes, screenLoc = screenLoc, screenCenter = screenRes/2 + screenLoc;


    public Vec2i mousePos { get; private set; }
    public Vec2i mouseDelta { get; private set; }
    public bool cursorOffScreen => mousePos.x < screenLoc.x || mousePos.y < screenLoc.y || mousePos.x >= screenLoc.x + screenRes.x || mousePos.y >= screenLoc.y + screenRes.y;


    public bool KeyHelt(Keys key)
        => keyState.Contains(key);

    public bool KeyDown(Keys key)
        => !lastKeyState.Contains(key) && keyState.Contains(key);

    public bool KeyUp(Keys key)
        => lastKeyState.Contains(key) && !keyState.Contains(key);


    internal void CB_OnKeyDown(Keys key)
    {
        if(!keyState.Contains(key))
            additionPending.Add(key);
    }

    internal void CB_OnKeyUp(Keys key)
    {
        if(keyState.Contains(key))
            removalPending.Add(key);
    }

    internal void Tick()
    {
        lastKeyState.Clear();
        lastKeyState.UnionWith(keyState);

        foreach(Keys key in removalPending)
            keyState.Remove(key);

        foreach(Keys key in additionPending)
            keyState.Add(key);

        additionPending.Clear();
        removalPending.Clear();

        if(lockCursor)
        {
            mouseDelta = (Vec2i)Cursor.Position - screenCenter;
            mousePos = Cursor.Position = screenCenter;
        }
        else
        {
            mouseDelta = mousePos - Cursor.Position;
            mousePos = Cursor.Position;
        }
    }
}