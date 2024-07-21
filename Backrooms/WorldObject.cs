using System;
using Backrooms.InputSystem;

namespace Backrooms;

public class WorldObject
{
    public const float MaxInteractionDist = .75f;
    public const float SqrMaxInteractionDist = MaxInteractionDist * MaxInteractionDist;
    public const float MinDotAtMaxDist = .9f;
    public const float MinDotAtMinDist = .6f;


    public readonly SpriteRenderer sprRend;
    public readonly Camera cam;
    public readonly Input input;
    public readonly Window win;
    public readonly Renderer rend;
    public Action onInteract;


    public WorldObject(Renderer rend, Window win, Camera cam, Input input, Vec2f pos, Vec2f size, UnsafeGraphic graphic, Action onInteract)
    {
        this.rend = rend;
        this.win = win;
        this.cam = cam;
        this.input = input;

        sprRend = new(pos, size, graphic);
        rend.sprites.Add(sprRend);

        win.tick += Tick;
    }


    private void Tick(float dt)
    {
        Vec2f camToObj = sprRend.pos - cam.pos;
        float sqrDist = camToObj.sqrLength;
        if(sqrDist > SqrMaxInteractionDist)
            return;

        float dot = Vec2f.Dot(camToObj, Vec2f.FromAngle(cam.angle));
        float minDot = Utils.Map(sqrDist, Utils.Sqr(sprRend.size.x), SqrMaxInteractionDist, MinDotAtMinDist, MinDotAtMaxDist);

        if(dot > minDot)
            return;

        if(input.KeyDown(InputAction.Interact))
            onInteract();
    }
}