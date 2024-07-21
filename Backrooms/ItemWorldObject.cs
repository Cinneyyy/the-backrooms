using Backrooms.InputSystem;

namespace Backrooms;

public class ItemWorldObject : WorldObject
{
    public const float HolderSize = .4f;
    public const float ItemSize = .25f;
    public const float ItemOffset = -.04f;

    public readonly SpriteRenderer itemRend;


    public ItemWorldObject(Renderer rend, Window win, CameraController cam, Input input, Vec2f pos, UnsafeGraphic holderGraphic, UnsafeGraphic itemGraphic)
        : base(rend, win, cam, input, pos, new(holderGraphic.whRatio * HolderSize, HolderSize), holderGraphic, null)
    {
        onInteract = OnInteract;

        sprRend.elevation = (HolderSize - 1f)/2f;
        itemRend = new(pos, new(itemGraphic.whRatio * ItemSize, ItemSize), itemGraphic) {
            elevation = HolderSize + (ItemSize - 1f)/2f + ItemOffset
        };

        rend.sprites.Add(itemRend);
    }


    private void OnInteract()
        => itemRend.enabled ^= true;
}