using Backrooms.InputSystem;
using Backrooms.ItemManagement;

namespace Backrooms;

public class ItemWorldObject : WorldObject
{
    public const float HolderSize = .4f;
    public const float ItemSize = .25f;
    public const float ItemOffset = -.04f;

    public readonly Inventory inv;
    public readonly SpriteRenderer itemRend;
    public readonly Item item;


    public ItemWorldObject(Renderer rend, Window win, CameraController cam, Input input, Inventory inv, Vec2f pos, UnsafeGraphic holderGraphic, Item item)
        : base(rend, win, cam, input, pos, new(holderGraphic.whRatio * HolderSize, HolderSize), holderGraphic, null)
    {
        this.inv = inv;
        this.item = item;
        onInteract = OnInteract;

        sprRend.elevation = (HolderSize - 1f)/2f;
        itemRend = new(pos, new(item.graphic.whRatio * ItemSize, ItemSize), item.graphic) {
            elevation = HolderSize + (ItemSize - 1f)/2f + ItemOffset
        };

        rend.sprites.Add(itemRend);

        itemRend.importance = 1;
        sprRend.importance = 0;
    }


    protected override void OnDispose()
        => rend.sprites.Remove(itemRend);


    private void OnInteract()
    {
        if(itemRend.enabled)
        {
            itemRend.enabled = false;
            inv.AddItem(item);
        }
    }
}