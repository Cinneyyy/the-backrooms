using System.Drawing;

namespace Backrooms.Gui;

public abstract class GuiElement(string name, Vec2f location, Vec2f size, Anchor anchor = Anchor.C)
{
    public GuiGroup group;
    public bool enabled = true;
    public string name = name;

    private Vec2f _location = location, _size = size;
    private Anchor _anchor = anchor;


    public Renderer rend => group.rend;
    public Vec2i screenLocation { get; private set; }
    public Vec2f screenLocationF { get; private set; }
    public Vec2i screenSize { get; private set; }
    public Vec2f screenSizeF { get; private set; }
    public Vec2f location
    {
        get => _location;
        set {
            _location = value;
            ReloadScreenLocation();
        }
    }
    public Vec2f size
    {
        get => _size;
        set {
            _size = value;
            ReloadScreenSize();
        }
    }
    public Anchor anchor
    {
        get => _anchor;
        set {
            _anchor = value;
            ReloadScreenLocation();
        }
    }
    public abstract bool isUnsafe { get; }
    public abstract bool isSafe { get; }


    public GuiElement(string name, Vec2f location, Vec2f size, GuiGroup group) : this(name, location, size)
    {
        this.group = group;
        ReloadScreenDimensions();
    }

    public GuiElement(string name, Vec2f location, Vec2f size, Anchor anchor, GuiGroup group) : this(name, location, size, anchor)
    {
        this.group = group;
        ReloadScreenDimensions();
    }


    public void ReloadScreenDimensions()
    {
        // Size before location, because size is involved in location calculation via anchor
        ReloadScreenSize();
        ReloadScreenLocation();
        ScreenDimensionsChanged();
    }

    public void ReloadScreenSize()
    {
        screenSizeF = size * rend.virtRes.y;
        screenSize = screenSizeF.Floor();
    }

    public void ReloadScreenLocation()
    {
        screenLocationF = location * rend.virtRes + group.screenAnchor - screenSizeF * (Vec2f)(anchor switch {
            Anchor.C => new(.5f, .5f),
            Anchor.T => new(.5f, 0f),
            Anchor.B => new(.5f, 1f),
            Anchor.L => new(0f, .5f),
            Anchor.R => new(1f, .5f),
            Anchor.TL => new(0f, 0f),
            Anchor.TR => new(1f, 0f),
            Anchor.BL => new(0f, 1f),
            Anchor.BR => new(1f, 1f),
            _ => throw new($"Invalid GUI anchor ;; {anchor} ;; {(byte)anchor}")
        });
        screenLocation = screenLocationF.Floor();
    }


    public unsafe abstract void DrawUnsafe(byte* scan, int stride, int w, int h);
    public abstract void DrawSafe(Graphics g);
    public virtual void OnAddedToGroup() { }


    protected virtual void ScreenDimensionsChanged() { }
}