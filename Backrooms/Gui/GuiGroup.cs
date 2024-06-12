using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Backrooms.Gui;

public class GuiGroup : IEnumerable<GuiElement>
{
    public const MouseButtons InteractMB = MouseButtons.Left;

    public Renderer rend;
    public Input input;
    public string name;
    public bool enabled;
    public bool calculateLocationUsingWidth;
    public event Action<float> persistentTick, groupEnabledTick;

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
    public Vec2f sizeRatioFactor { get; private set; }
    public Vec2f screenFactor { get; private set; }
    public Vec2f screenOffset { get; private set; }
    public bool mbHelt => input.MbHelt(InteractMB);
    public bool mbDown => input.MbDown(InteractMB);
    public IEnumerable<GuiElement> allElements => unsafeElements.Concat(safeElements);


    public GuiGroup(Renderer rend, string name, bool locationUsingWidth, bool enabled = true)
    {
        this.rend = rend;
        input = rend.input;
        this.name = name;
        this.enabled = enabled;
        calculateLocationUsingWidth = locationUsingWidth;
        rend.window.tick += Tick;

        ReloadScreenDimensions();
        rend.dimensionsChanged += ReloadScreenDimensions;
    }


    public void Add(GuiElement elem)
    {
        elem.group = this;

        if(elem.isUnsafe) unsafeElements.Add(elem);
        if(elem.isSafe) safeElements.Add(elem);

        elem.OnAddedToGroup();
        elem.ReloadScreenDimensions();
    }

    public void Remove(GuiElement elem)
    {
        elem.group = null;

        if(elem.isUnsafe) unsafeElements.Remove(elem);
        if(elem.isSafe) safeElements.Remove(elem);

        elem.OnRemovedFromGroup();
    }

    public unsafe void DrawUnsafeElements(byte* scan, int stride, int w, int h)
    {
        if(!enabled)
            return;

        foreach(GuiElement elem in unsafeElements)
            if(elem.enabled)
                elem.DrawUnsafe(scan, stride, w, h);
    }

    public void DrawSafeElements(Graphics g)
    {
        if(!enabled)
            return;

        foreach(GuiElement elem in safeElements)
            if(elem.enabled)
                elem.DrawSafe(g);
    }

    public GuiElement GetUnsafeElement(Index idx) => unsafeElements[idx];
    public T GetUnsafeElement<T>(Index idx) where T : GuiElement => GetUnsafeElement(idx) as T;

    public GuiElement GetSafeElement(Index idx) => safeElements[idx];
    public T GetSafeElement<T>(Index idx) where T : GuiElement => GetSafeElement(idx) as T;

    public GuiElement GetElement(string name) => (from e in allElements
                                                   where e.name == name
                                                   select e).FirstOrDefault();
    public T GetElement<T>(string name) where T : GuiElement => GetElement(name) as T;

    public IEnumerator<GuiElement> GetEnumerator() => allElements.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    private void ReloadScreenDimensions()
    {
        screenStep = 1f / (Vec2f)rend.virtRes;
        (screenFactor, screenOffset, sizeRatioFactor) = calculateLocationUsingWidth ? (rend.virtRes, Vec2f.zero, Vec2f.one) : (new(rend.virtRes.y), new((rend.virtRes.x - rend.virtRes.y)/2f, 0f), new(1f / rend.virtRatio, 1f));

        foreach(GuiElement e in allElements)
            e.ReloadScreenDimensions();
    }

    private void Tick(float dt)
    {
        persistentTick?.Invoke(dt);

        if(enabled)
            groupEnabledTick?.Invoke(dt);
    }
}