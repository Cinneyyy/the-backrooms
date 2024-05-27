using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Backrooms.Gui;

public class GuiGroup : IEnumerable<GuiElement>
{
    public Renderer rend;
    public string name;
    public bool enabled;

    private readonly List<GuiElement> unsafeElements = [], safeElements = [];
    private Vec2f _screenAnchor;


    public Vec2f screenStep { get; private set; }
    public Vec2f screenAnchor
    {
        get => _screenAnchor;
        set {
            _screenAnchor = value;
            ReloadScreenDimensions();
        }
    }


    public GuiGroup(Renderer rend, string name, bool enabled = true)
    {
        this.rend = rend;
        this.name = name;
        this.enabled = enabled;

        rend.dimensionsChanged += ReloadScreenDimensions;
    }


    public void Add(GuiElement element)
    {
        element.group = this;

        if(element.isUnsafe) unsafeElements.Add(element);
        if(element.isSafe) safeElements.Add(element);

        element.OnAddedToGroup();
        element.ReloadScreenDimensions();
    }

    public void Remove(GuiElement element)
    {
        element.group = null;

        if(element.isUnsafe) unsafeElements.Remove(element);
        if(element.isSafe) safeElements.Remove(element);
    }

    public unsafe void DrawUnsafeElements(byte* scan, int stride, int w, int h)
    {
        if(!enabled)
            return;

        foreach(GuiElement element in unsafeElements)
            if(element.enabled)
                element.DrawUnsafe(scan, stride, w, h);
    }

    public void DrawSafeElements(Graphics g)
    {
        if(!enabled)
            return;

        foreach(GuiElement element in safeElements)
            if(element.enabled)
                element.DrawSafe(g);
    }

    public GuiElement GetUnsafeElement(Index idx) => unsafeElements[idx];
    public T GetUnsafeElement<T>(Index idx) where T : GuiElement => GetUnsafeElement(idx) as T;

    public GuiElement GetSafeElement(Index idx) => safeElements[idx];
    public T GetSafeElement<T>(Index idx) where T : GuiElement => GetSafeElement(idx) as T;

    public GuiElement FindElement(string name) => (from e in unsafeElements.Concat(safeElements)
                                                   where e.name == name
                                                   select e).FirstOrDefault();
    public T FindElement<T>(string name) where T : GuiElement => FindElement(name) as T;

    public IEnumerator<GuiElement> GetEnumerator() => unsafeElements.Concat(safeElements).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    private void ReloadScreenDimensions()
    {
        screenStep = 1f / (Vec2f)rend.virtRes;
        foreach(GuiElement e in unsafeElements.Concat(safeElements))
            e.ReloadScreenDimensions();
    }
}