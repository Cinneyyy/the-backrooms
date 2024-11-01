using Backrooms.Gui;

namespace Backrooms.ItemManagement;

public class InvSlot(Vec2i index)
{
    public readonly Vec2i index = index;

    private Item _item;


    public Item item
    {
        get => _item;
        set {
            _item = value;
            invItemDisplay.graphic = item?.graphic;
        }
    }
    public bool isEmpty => item is null;
    public ImageElement invItemDisplay { get; private set; }
    public RectSolidColorElement invItemBackground { get; private set; }


    public void SetGuiElements(ImageElement display, RectSolidColorElement background)
    {
        Assert(Log.Info, invItemDisplay is null && invItemBackground is null, "invItemDisplay & -Background are already defined");

        invItemDisplay = display;
        invItemBackground = background;
    }
}