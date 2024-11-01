using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Backrooms.Gui;

public class GuiGroup : IEnumerable<GuiElement>
{
    public const Keys InteractionButton = Keys.LButton;

    public Renderer rend;
    public Input input;
    public string name;
    public bool enabled;
    public event Action<float> persistentTick, groupEnabledTick;

    private readonly List<GuiElement> unsafeElements = [], safeElements = [], neutralElements = [];
    private Vec2f _screenAnchor;
    private bool _fullScreenLocation;


    public Vec2f screenStep { get; private set; }
    public Vec2f position
    {
        get => _screenAnchor;
        set {
            _screenAnchor = value;
            ReloadScreenDimensions();
        }
    }
    public bool fullScreenLocation
    {
        get => _fullScreenLocation;
        set {
            _fullScreenLocation = value;
            ReloadScreenDimensions();
        }
    }
    public Vec2f screenRes { get; private set; }
    public Vec2f screenOffset { get; private set; }
    public Vec2f guiToVirtRatio { get; private set; }
    public bool mbHelt => input.KeyHelt(InteractionButton);
    public bool mbDown => input.KeyDown(Keys.LButton);
    public IEnumerable<GuiElement> allElements => unsafeElements.Union(safeElements).Union(neutralElements);


    public GuiGroup(Renderer rend, string name, bool fullScreenLocation, bool enabled = true)
    {
        this.rend = rend;
        input = rend.input;
        this.name = name;
        foreach(char c in name)
            if(char.IsUpper(c) || !(char.IsLetter(c) || c == '_'))
                Out(Log.Info, $"The GuiGroup name {name} should be changed to snake_case, for consistency");
        this.enabled = enabled;
        this.fullScreenLocation = fullScreenLocation;
        rend.window.tick += Tick;

        ReloadScreenDimensions();
        rend.dimensionsChanged += ReloadScreenDimensions;
    }


    public void Add(GuiElement elem)
    {
        if(elem.isUnsafe) unsafeElements.Add(elem);
        if(elem.isSafe) safeElements.Add(elem);
        if(!elem.isSafe && !elem.isSafe) neutralElements.Add(elem);

        elem.group = this;
        elem.OnAddedToGroup();
        elem.ReloadScreenDimensions();
    }

    public void AddManySafe(IEnumerable<GuiElement> elems)
    {
        safeElements.AddRange(elems);

        foreach(GuiElement elem in elems)
        {
            if(!elem.isSafe)
                throw new($"Attempted to add non-safe element '{elem.name}' to GuiGroup '{name}'");

            elem.group = this;
            elem.OnAddedToGroup();
            elem.ReloadScreenDimensions();
        }
    }

    public void AddManyUnsafe(IEnumerable<GuiElement> elems)
    {
        unsafeElements.AddRange(elems);

        foreach(GuiElement elem in elems)
        {
            if(!elem.isUnsafe)
                throw new($"Attempted to add non-unsafe element '{elem.name}' to GuiGroup '{name}'");

            elem.group = this;
            elem.OnAddedToGroup();
            elem.ReloadScreenDimensions();
        }
    }

    public void AddMany(IEnumerable<GuiElement> elems)
    {
        foreach(GuiElement elem in elems)
            Add(elem);
    }

    public void Remove(GuiElement elem)
    {
        elem.group = null;

        if(elem.isUnsafe) unsafeElements.Remove(elem);
        if(elem.isSafe) safeElements.Remove(elem);
        if(!elem.isSafe && !elem.isUnsafe) neutralElements.Remove(elem);

        elem.OnRemovedFromGroup();
    }

    public void RemoveMany(IEnumerable<GuiElement> elems)
    {
        foreach(GuiElement elem in elems)
            Remove(elem);
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
    public GuiElement GetUnsafeElement(string name) => unsafeElements.Find(e => e.name == name);
    public T GetUnsafeElement<T>(string name) where T : GuiElement => GetUnsafeElement(name) as T;
    public GuiElement GetUnsafeElement(Regex pattern) => unsafeElements.Find(e => pattern.IsMatch(e.name));
    public T GetUnsafeElement<T>(Regex pattern) where T : GuiElement => GetUnsafeElement(pattern) as T;

    public GuiElement GetSafeElement(Index idx) => safeElements[idx];
    public T GetSafeElement<T>(Index idx) where T : GuiElement => GetSafeElement(idx) as T;
    public GuiElement GetSafeElement(string name) => safeElements.Find(e => e.name == name);
    public T GetSafeElement<T>(string name) where T : GuiElement => GetSafeElement(name) as T;
    public GuiElement GetSafeElement(Regex pattern) => safeElements.Find(e => pattern.IsMatch(e.name));
    public T GetSafeElement<T>(Regex pattern) where T : GuiElement => GetSafeElement(pattern) as T;

    public GuiElement GetElement(string name) => allElements.Where(e => e.name == name).FirstOrDefault();
    public T GetElement<T>(string name) where T : GuiElement => GetElement(name) as T;
    public GuiElement GetElement(Regex pattern) => allElements.Where(e => pattern.IsMatch(e.name)).FirstOrDefault();
    public T GetElement<T>(Regex pattern) where T : GuiElement => GetElement(pattern) as T;

    public IEnumerable<GuiElement> GetElements(Regex pattern) => allElements.Where(e => pattern.IsMatch(e.name));
    public IEnumerable<T> GetElements<T>(Regex pattern) where T : GuiElement => allElements.Where(e => pattern.IsMatch(e.name)).Select(e => e as T);

    public IEnumerator<GuiElement> GetEnumerator() => allElements.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    private void ReloadScreenDimensions()
    {
        if(fullScreenLocation)
        {
            screenRes = rend.virtRes;
            screenOffset = Vec2f.zero;
            guiToVirtRatio = Vec2f.one;
        }
        else
        {
            screenRes = new(rend.virtRes.y);
            screenOffset = new((rend.virtRes.x - rend.virtRes.y)/2f, 0f);
            guiToVirtRatio = new(1f / rend.virtRatio, 1f);
        }

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