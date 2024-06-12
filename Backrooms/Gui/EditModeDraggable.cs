using System;
using System.Drawing;
using System.Windows.Forms;

namespace Backrooms.Gui;

public unsafe class EditModeDraggable(ColorBlock colors, Vec2f location, Vec2f size, GuiElement targetElem, bool constrainX, bool constrainY, GuiGroup group, EditModeDraggable.DragAction drag)
{
    public delegate void DragAction(Vec2f absolute, Vec2f delta);


    public Vec2f location = location, size = size;
    public GuiGroup group = group;
    public GuiElement targetElem = targetElem;
    public ColorBlock colors = colors;
    public readonly DragAction drag = drag;
    public bool constrainX = constrainX, constrainY = constrainY;
    public bool isDragging;
    public Vec2f startDragLocation;


    public void DrawAndProcess(byte* scan0, int stride, int w, int h)
    {
        Vec2i screenSize = (size * group.rend.virtRes.y).Floor();
        Vec2i screenLoc = (location * group.screenFactor + group.screenOffset + group.screenAnchor).Floor();

        bool isHovering = group.input.ContainsCursor(screenLoc, screenSize);
        
        if(group.input.MbDown(MouseButtons.Left) && isHovering)
        {
            isDragging = true;
            startDragLocation = location;
        }

        if(isDragging && group.input.MbUp(MouseButtons.Left))
            isDragging = false;

        if(isDragging)
        {
            Vec2f drag = (group.input.normMousePos - new Vec2f(targetElem.size.x/2f, 0f)) / group.sizeRatioFactor;

            if(constrainX) drag.x = location.x;
            if(constrainY) drag.y = location.y;

            this.drag(drag, drag-startDragLocation);
            location = drag;
        }

        Color color = colors.GetColor(isHovering, isDragging);

        screenLoc = (location * group.screenFactor + group.screenOffset + group.screenAnchor).Floor();

        Vec2i tl = screenLoc - screenSize/2, br = screenLoc + screenSize/2;
        int x0 = Utils.Clamp(tl.x, 0, group.rend.virtRes.x), x1 = Utils.Clamp(br.x, 0, group.rend.virtRes.x);
        int y0 = Utils.Clamp(tl.y, 0, group.rend.virtRes.y), y1 = Utils.Clamp(br.y, 0, group.rend.virtRes.y);

        scan0 += x0*3 + y0*stride;
        stride -= (x1-x0) * 3;

        for(int x = x0; x < x1; x++)
        {
            for(int y = y0; y < y1; y++)
            {
                *scan0++ = color.B;
                *scan0++ = color.G;
                *scan0++ = color.R;
            }

            scan0 += stride;
        }
    }
}