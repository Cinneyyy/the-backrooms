using System;
using System.Drawing;
using System.Reflection;

namespace Backrooms.Gui;

public abstract class GuiElement
{
    public GuiGroup group;
    public string name;
    public readonly GuiElementAttribute safetyAttr;

    private Vec2f _location, _size;
    private Anchor _anchor;
    private bool _enabled = true;


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
    public bool enabled
    {
        get => _enabled;
        set {
            if(_enabled == value)
                return;

            _enabled = value;
            OnToggle();

            if(value)
                OnEnable();
            else
                OnDisable();
        }
    }
    public bool isSafe => safetyAttr.isSafe;
    public bool isUnsafe => safetyAttr.isUnsafe;


    public GuiElement(string name, Vec2f location, Vec2f size, Anchor anchor = Anchor.C)
    {
        this.name = name;
        _location = location;
        _size = size;
        _anchor = anchor;

        safetyAttr = GetType().GetCustomAttribute<GuiElementAttribute>();
        if(safetyAttr is null)
            throw new($"All types derived from GuiElement must have GuiElementAttribute");
    }


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
        screenLocationF = location * group.screenFactor + group.screenOffset + group.screenAnchor - screenSizeF * anchor.ToOffset();
        screenLocation = screenLocationF.Floor();
    }

    public unsafe virtual void DrawUnsafe(byte* scan, int stride, int w, int h) => throw new NotImplementedException("DrawUnsafe has not been implemented");
    public virtual void DrawSafe(Graphics g) => throw new NotImplementedException("DrawSafe has not been implemented");
    public virtual void OnAddedToGroup() { }
    public virtual void OnRemovedFromGroup() { }


    protected virtual void ScreenDimensionsChanged() { }
    protected virtual void OnEnable() { }
    protected virtual void OnDisable() { }
    protected virtual void OnToggle() { }
}