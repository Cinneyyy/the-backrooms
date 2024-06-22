namespace Backrooms.ItemManagement;

public abstract class Item(string name, string desc, bool consumeOnUse, UnsafeGraphic graphic)
{
    public readonly string name = name, desc = desc;
    public readonly bool consumeOnUse = consumeOnUse;
    public UnsafeGraphic graphic = graphic;


    public void Use(InvSlot itemSlot)
    {
        if(consumeOnUse)
            itemSlot.item = null;

        OnUse();
    }


    protected abstract void OnUse();
}