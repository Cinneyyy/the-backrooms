using System.Collections.Generic;
using System.Windows.Forms;

namespace Backrooms;

public partial class Input(Renderer rend, Vec2i screenLoc, bool lockCursor)
{
    public bool lockCursor = lockCursor;

    private readonly HashSet<Keys> additionPending = [], removalPending = [], keyState = [], lastKeyState = [];
    private readonly HashSet<MouseButtons> additionPendingMb = [], removalPendingMb = [], mbState = [], lastMbState = [];
    private readonly Vec2i screenLoc = screenLoc;
    private readonly Renderer rend = rend;
    private Vec2i screenRes = rend.physRes, screenCenter = rend.physCenter + screenLoc;


    public Vec2i mousePos { get; private set; }
    public Vec2i mouseDelta { get; private set; }
    public Vec2i virtMousePos { get; private set; }
    public Vec2i virtMouseDelta { get; private set; }
    public Vec2f normMousePos { get; private set; }
    public Vec2f normMouseDelta { get; private set; }
    public bool cursorOffScreen => mousePos.x < screenLoc.x || mousePos.y < screenLoc.y || mousePos.x >= screenLoc.x + screenRes.x || mousePos.y >= screenLoc.y + screenRes.y;


    public bool KeyHelt(Keys key)
        => keyState.Contains(key);
    public bool KeyDown(Keys key)
        => !lastKeyState.Contains(key) && keyState.Contains(key);
    public bool KeyUp(Keys key)
        => lastKeyState.Contains(key) && !keyState.Contains(key);

    public bool MbHelt(MouseButtons mb)
        => mbState.Contains(mb);
    public bool MbDown(MouseButtons mb)
        => !lastMbState.Contains(mb) && mbState.Contains(mb);
    public bool MbUp(MouseButtons mb)
        => lastMbState.Contains(mb) && !mbState.Contains(mb);

    public bool ContainsCursor(Vec2f loc, Vec2f size)
        => Utils.InsideRect(loc, size, virtMousePos);
    public bool ContainsCursorCentered(Vec2f loc, Vec2f size)
        => Utils.InsideRectCentered(loc, size, virtMousePos);

    public bool ContainsNormCursor(Vec2f loc, Vec2f size, Vec2f? mPosMul = null)
        => Utils.InsideRect(loc, size, normMousePos * (mPosMul ?? Vec2f.one));
    public bool ContainsNormCursorCentered(Vec2f loc, Vec2f size, Vec2f? mPosMul = null)
        => Utils.InsideRectCentered(loc, size, normMousePos * (mPosMul ?? Vec2f.one));


    internal void OnUpdateDimensions(Renderer rend)
    {
        screenRes = rend.physRes;
        screenCenter = rend.physCenter + screenLoc;
    }

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

    internal void CB_OnCursorDown(MouseButtons mb)
    {
        if(!mbState.Contains(mb))
            additionPendingMb.Add(mb);
    }
    internal void CB_OnCursorUp(MouseButtons mb)
    {
        if(mbState.Contains(mb))
            removalPendingMb.Add(mb);
    }

    internal void Tick()
    {
        lastKeyState.Clear();
        lastMbState.Clear();
        lastKeyState.UnionWith(keyState);
        lastMbState.UnionWith(mbState);

        foreach(Keys key in removalPending)
            keyState.Remove(key);
        foreach(Keys key in additionPending)
            keyState.Add(key);

        foreach(MouseButtons mb in removalPendingMb)
            mbState.Remove(mb);
        foreach(MouseButtons mb in additionPendingMb)
            mbState.Add(mb);

        additionPending.Clear();
        additionPendingMb.Clear();
        removalPending.Clear();
        removalPendingMb.Clear();

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

        virtMousePos = (Vec2i)((mousePos - screenLoc) * rend.downscaleFactor);
        virtMouseDelta = (Vec2i)(virtMouseDelta * rend.downscaleFactor);
        normMousePos = (Vec2f)virtMousePos/rend.virtRes;
        normMouseDelta = (Vec2f)virtMouseDelta/rend.virtRes;
    }
}