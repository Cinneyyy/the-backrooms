using System;
using System.Drawing;
using System.Windows.Forms;
using Backrooms.Gui;
using Backrooms.InputSystem;

namespace Backrooms.ItemManagement;

public class Inventory
{
    public readonly Vec2i size;
    public readonly InvSlot[,] slots;
    public readonly GuiGroup gui;
    public readonly Vec2f guiBounds, guiTL, guiBR;
    public readonly float slotSize;
    public readonly Input input;
    public readonly Window win;
    public readonly float guiScale;
    public readonly Game game;
    public ColorBlock colors;

    private bool _enabled;
    private InvSlot hoveredSlot;
    private readonly Vec2f invOffset = new(0f, .1f);
    private InvSlot selectedSlot;


    public bool enabled
    {
        get => _enabled;
        set {
            _enabled = value;
            gui.enabled = value;
            win.SetCursor(value);
        }
    }


    public Inventory(Window win, Renderer rend, Game game, Input input, Vec2i size, ColorBlock slotColors, float guiScale = .75f)
    {
        this.input = input;
        this.game = game;
        this.win = win;
        win.tick += Tick;

        this.size = size;
        this.guiScale = guiScale;
        colors = slotColors;
        slots = new InvSlot[size.x, size.y];
        slots.Populate((x, y) => new(new(x, y)));

        gui = new(rend, "inventory", false, false) {
            new RectSolidColorElement("background", Color.FromArgb(0x50, Color.Black), true, Vec2f.half, ((Vec2f)rend.virtRes) / rend.virtRes.min),
            new TextElement("title", "< Inventory >", Resources.fonts["cascadia_code"], 20f, Color.Yellow, Vec2f.half, new(.5f, 1/3f), Vec2f.zero)
        };

        GuiElement[] guiElements = new GuiElement[size.x * size.y * 2];
        slotSize = guiScale / size.max;
        guiBounds = new Vec2f(slotSize / rend.virtRatio, slotSize) * (Vec2f)size;
        guiTL = Vec2f.half - guiBounds/2f + invOffset;
        guiBR = guiTL + guiBounds;

        Vec2f slotSizeVec = new(slotSize * .925f);
        Vec2f baseSlotLoc = Vec2f.half - (slotSize * (Vec2f)size)/2f + new Vec2f(slotSize/2f) + invOffset;

        for(int x = 0; x < size.x; x++)
            for(int y = 0; y < size.y; y++)
            {
                int index = 2 * (x + size.x * y);
                Vec2f loc = baseSlotLoc + new Vec2f(x, y) * slotSize;

                guiElements[index] = new RectSolidColorElement($"{x}_{y}_bg", slotColors.normal, true, loc, slotSizeVec);
                guiElements[index+1] = new ImageElement($"{x}_{y}_graphic", graphic: null, Color.White, loc, slotSizeVec);
                slots[x, y].SetGuiElements(guiElements[index+1] as ImageElement, guiElements[index] as RectSolidColorElement);
            }

        gui.AddManyUnsafe(guiElements);
        rend.guiGroups.Add(gui);
    }


    public Item this[int x, int y]
    {
        get => slots[x, y].item;
        set => slots[x, y].item = value;
    }

    public Item this[Vec2i idx]
    {
        get => this[idx.x, idx.y];
        set => this[idx.x, idx.y] = value;
    }


    public void Swap(Vec2i a, Vec2i b)
        => (this[a], this[b]) = (this[b], this[a]);

    /// <summary>Returns whether or not the insertion was successful</summary>
    public bool AddItem(Item item)
    {
        for(int y = 0; y < size.y; y++)
            for(int x = 0; x < size.x; x++)
                if(slots[x, y].isEmpty)
                {
                    slots[x, y].item = item;
                    return true;
                }

        return false;
    }
    /// <summary>Returns whether or not the insertion was successful</summary>
    public bool AddItem(string itemId)
        => AddItem(Item.items[itemId]);

    /// <summary>Returns whether or not the slot was empty before the insertion</summary>
    public bool SetItem(Vec2i slot, Item item)
    {
        bool wasEmpty = IsEmpty(slot);
        this[slot] = item;
        return wasEmpty;
    }
    /// <summary>Returns whether or not the slot was empty before the insertion</summary>
    public bool SetItem(int x, int y, Item item)
        => SetItem(new(x, y), item);

    public bool IsEmpty(Vec2i slot)
        => slots[slot.x, slot.y].isEmpty;
    public bool IsEmpty(int x, int y)
        => IsEmpty(new(x, y));
    public bool IsEmpty()
    {
        for(int x = 0; x < size.x; x++)
            for(int y = 0; y < size.y; y++)
                if(!IsEmpty(x, y))
                    return false;

        return true;
    }

    public void ForEachSlot(Action<InvSlot> action)
    {
        foreach(InvSlot slot in slots)
            action(slot);
    }

    public void Clear()
        => ForEachSlot(slot => slot.item = null);


    private void Tick(float dt)
    {
        if(input.KeyDown(Keys.I))
            enabled ^= true;

        if(!enabled)
            return;

        if(input.ContainsNormCursorCentered(Vec2f.half + invOffset, guiBounds))
        {
            Vec2f mPos = input.normMousePos;
            Vec2i selectedSlot = new Vec2f(
                Utils.Map(mPos.x, guiTL.x, guiBR.x, 0f, size.x),
                Utils.Map(mPos.y, guiTL.y, guiBR.y, 0f, size.y)).Floor();

            InvSlot slot = slots[selectedSlot.x, selectedSlot.y];

            slot.invItemBackground.color = input.KeyHelt(Keys.LButton) ? colors.select : colors.hover;
            if(input.KeyDown(Keys.Left))
                ClickSlot(slot);
            else if(input.KeyDown(Keys.Right))
                if(this.selectedSlot is not null)
                    this.selectedSlot = null;
                else          
                    slot.item?.Use(slot, game);

            if(slot != hoveredSlot)
            {
                if(hoveredSlot is not null)
                    hoveredSlot.invItemBackground.color = colors.normal;
                hoveredSlot = slot;
            }
        }
        else if(hoveredSlot is not null)
        {
            hoveredSlot.invItemBackground.color = colors.normal;
            hoveredSlot = null;
        }
    }

    private void ClickSlot(InvSlot slot)
    {
        if(selectedSlot is null)
            selectedSlot = slot;
        else
        {
            Swap(selectedSlot.index, slot.index);
            selectedSlot = null;
        }
    }
}