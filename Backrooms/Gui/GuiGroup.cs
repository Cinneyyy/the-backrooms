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
    private readonly List<EditModeDraggable> editModeElements = [];
    private Vec2f _screenAnchor;
    private bool _editMode;


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
    public bool mbHelt => !editMode && input.MbHelt(InteractMB);
    public bool mbDown => !editMode && input.MbDown(InteractMB);
    public bool editMode
    {
        get => _editMode;
        set {
            if(_editMode == value)
                return;

            _editMode = value;
            if(value)
                EnterEditMode();
            else
                editModeElements.Clear();
        }
    }

    private IEnumerable<GuiElement> allElements => unsafeElements.Concat(safeElements);


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

        if(editMode)
        {
            foreach(EditModeDraggable draggable in editModeElements)
                draggable.DrawAndProcess(scan, stride, w, h);
        }

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

    public GuiElement FindElement(string name) => (from e in allElements
                                                   where e.name == name
                                                   select e).FirstOrDefault();
    public T FindElement<T>(string name) where T : GuiElement => FindElement(name) as T;

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
        if(input.KeyDown(Keys.F6))
            editMode ^= true;

        persistentTick?.Invoke(dt);

        if(enabled)
            groupEnabledTick?.Invoke(dt);
    }

    private void EnterEditMode()
    {
        foreach(GuiElement elem in allElements)
            AddEditModeItems(elem);
    }

    private void AddEditModeItems(GuiElement elem)
    {
        ColorBlock colors = new(Color.White, .9f, .6f, .3f, true, false);
        EditModeDraggable rightScaler = new(colors, elem.location + Anchor.T.ToOffset() * elem.size, new(.05f), elem, false, true, this, (a, d) => elem.size = elem.size with { x = elem.size.x + d.x });
        editModeElements.Add(rightScaler);
    }
}