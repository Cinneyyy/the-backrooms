using System;
using System.Drawing;
using System.Reflection;

namespace Backrooms.Gui;

public abstract class GuiElement
{
    public GuiGroup group;
    public string name;
    public readonly GuiElementAttribute safetyAttr;

    private Vec2f _location, _size, _anchor;
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
    public Vec2f anchor
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


    public GuiElement(string name, Vec2f location, Vec2f size, Vec2f? anchor = null)
    {
        this.name = name;
        _location = location;
        _size = size;
        _anchor = anchor ?? Vec2f.half;

        Type type = GetType();
        Assert(type.Name.EndsWith("element", StringComparison.OrdinalIgnoreCase), $"All GUI element type names should end with the 'Element' suffix for clarity");

        safetyAttr = type.GetCustomAttribute<GuiElementAttribute>();
        if(safetyAttr is null)
            throw new($"All types derived from GuiElement must have GuiElementAttribute");
    }


    public GuiElement(string name, Vec2f location, Vec2f size, GuiGroup group) : this(name, location, size)
    {
        this.group = group;
        ReloadScreenDimensions();
    }

    public GuiElement(string name, Vec2f location, Vec2f size, Vec2f anchor, GuiGroup group) : this(name, location, size, anchor)
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
        screenLocationF = location * group.screenRes + group.screenOffset + group.position - screenSizeF * anchor;
        screenLocation = screenLocationF.Floor();
    }

    public unsafe virtual void DrawUnsafe(byte* scan, int stride, int w, int h) => throw new NotImplementedException($"GUI type {GetType().FullName} does not implement the unsafe draw call");
    public virtual void DrawSafe(Graphics g) => throw new NotImplementedException($"GUI type {GetType().FullName} does not implement the safe draw call");
    public virtual void OnAddedToGroup() { }
    public virtual void OnRemovedFromGroup() { }


    protected virtual void ScreenDimensionsChanged() { }
    protected virtual void OnEnable() { }
    protected virtual void OnDisable() { }
    protected virtual void OnToggle() { }
}