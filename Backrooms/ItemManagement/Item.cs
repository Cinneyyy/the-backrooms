using System.Collections.Generic;
using Backrooms.ItemManagement.Items;

namespace Backrooms.ItemManagement;

public abstract class Item(string name, string desc, bool consumeOnUse, UnsafeGraphic graphic)
{
    public readonly string name = name, desc = desc;
    public readonly bool consumeOnUse = consumeOnUse;
    public UnsafeGraphic graphic = graphic;

    public static readonly Dictionary<string, Item> items = new() {
        ["vodka"] = new Consumable("Vodka", "Makes the time down here bearable", -10f, -10f, -5f, -15f, null),
        ["oli"] = new Consumable("Oli", "Schläft gleich ein", -10f, -10f, -5f, -15f, null)
    };


    static Item()
    {
        static void load_sprites()
        {
            foreach((string id, Item item) in items)
                item.graphic = Resources.unsafeGraphics[id];
        }

        if(Resources.finishedInit) load_sprites();
        else Resources.onFinishInit += load_sprites;
    }


    public void Use(InvSlot itemSlot, Game game)
    {
        Out(Log.GameEvent, $"Used item '{name}' (ItemType: {GetType().Name})");

        if(consumeOnUse)
            itemSlot.item = null;

        OnUse(game);
    }


    protected abstract void OnUse(Game game);
}