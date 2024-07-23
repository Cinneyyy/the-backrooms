using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Backrooms;

public partial class Input
{
    public bool lockCursor;
    public KeyMap keyMap;

    private readonly HashSet<Keys> additionPending = [], removalPending = [], keyState = [], lastKeyState = [];
    private readonly Vec2i screenLoc;
    private readonly Renderer rend;
    private Vec2i screenRes, screenCenter;


    public Vec2i mousePos { get; private set; }
    public Vec2i mouseDelta { get; private set; }
    public Vec2i virtMousePos { get; private set; }
    public Vec2i virtMouseDelta { get; private set; }
    public Vec2f normMousePos { get; private set; }
    public Vec2f normMouseDelta { get; private set; }
    public bool cursorOffScreen => mousePos.x < screenLoc.x || mousePos.y < screenLoc.y || mousePos.x >= screenLoc.x + screenRes.x || mousePos.y >= screenLoc.y + screenRes.y;
    public bool anyKey => keyState.Count > 0;


    public Input(Renderer rend, Vec2i screenLoc, bool lockCursor)
    {
        this.lockCursor = lockCursor;
        this.screenLoc = screenLoc;
        this.rend = rend;

        screenRes = rend.physRes;
        screenCenter = rend.physCenter + screenLoc;

        keyMap = new(this) {
            [InputAction.MoveForward] = Keys.W,
            [InputAction.MoveBackward] = Keys.S,
            [InputAction.MoveLeft] = Keys.A,
            [InputAction.MoveRight] = Keys.D,
            [InputAction.Sprint] = Keys.ShiftKey,
            [InputAction.Interact] = Keys.E
        };
    }


    public bool KeyHelt(Keys key) => keyState.Contains(key);
    public bool KeyHelt(InputAction key) => keyMap.KeyHelt(key);
    public bool KeyDown(Keys key) => !lastKeyState.Contains(key) && keyState.Contains(key);
    public bool KeyDown(InputAction key) => keyMap.KeyDown(key);
    public bool KeyUp(Keys key) => lastKeyState.Contains(key) && !keyState.Contains(key);
    public bool KeyUp(InputAction key) => keyMap.KeyUp(key);

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

        Vec2i cp = (Vec2i)Cursor.Position;
        if(lockCursor)
        {
            mouseDelta = cp - screenCenter;
            mousePos = screenCenter;
            Cursor.Position = (Point)mousePos;
        }
        else
        {
            mouseDelta = mousePos - cp;
            mousePos = cp;
        }

        virtMousePos = (Vec2i)((mousePos - screenLoc) * rend.downscaleFactor);
        virtMouseDelta = (Vec2i)(virtMouseDelta * rend.downscaleFactor);
        normMousePos = (Vec2f)virtMousePos / rend.virtRes;
        normMouseDelta = (Vec2f)virtMouseDelta / rend.virtRes;
    }
}