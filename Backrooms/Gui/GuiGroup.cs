using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Backrooms.Gui;

public class GuiGroup
{
    public Renderer rend;

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


    public GuiGroup(Renderer rend)
    {
        this.rend = rend;
        rend.dimensionsChanged += ReloadScreenDimensions;
    }


    public void Add(GuiElement element)
    {
        element.group = this;
        (element.isUnsafe ? unsafeElements : safeElements).Add(element);
        element.ReloadScreenDimensions();
    }

    public void Remove(GuiElement element)
    {
        element.group = null;
        (element.isUnsafe ? unsafeElements : safeElements).Remove(element);
        element.ReloadScreenDimensions();
    }

    public unsafe void DrawUnsafeElements(byte* scan, int stride, int w, int h)
    {
        foreach(GuiElement element in unsafeElements)
            if(element.enabled)
                element.DrawUnsafe(scan, stride, w, h);
    }

    public void DrawSafeElements(Graphics g)
    {
        foreach(GuiElement element in safeElements)
            if(element.enabled)
                element.DrawSafe(g);
    }

    public GuiElement GetUnsafeElement(Index idx) => unsafeElements[idx];
    public GuiElement GetSafeElement(Index idx) => safeElements[idx];
    public GuiElement GetElement(string name) => (from e in safeElements.Concat(unsafeElements)
                                                 where e.name == name
                                                 select e).FirstOrDefault();


    private void ReloadScreenDimensions()
    {
        screenStep = 1f / (Vec2f)rend.virtRes;
        foreach(GuiElement e in unsafeElements)
            e.ReloadScreenDimensions();
    }
}