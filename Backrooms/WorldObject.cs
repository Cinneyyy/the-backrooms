using System;
using Backrooms.InputSystem;

namespace Backrooms;

public class WorldObject
{
    public const float MaxInteractionDist = 1.25f;
    public const float SqrMaxInteractionDist = MaxInteractionDist * MaxInteractionDist;
    public const float MinDotAtMaxDist = .985f;
    public const float MinDotAtMinDist = .825f;


    public readonly SpriteRenderer sprRend;
    public readonly CameraController cam;
    public readonly Input input;
    public readonly Window win;
    public readonly Renderer rend;
    public Action onInteract;


    public Vec2f pos
    {
        get => sprRend.pos;
        set => sprRend.pos = value;
    }
    public Vec2f size
    {
        get => sprRend.size;
        set => sprRend.size = value;
    }


    public WorldObject(Renderer rend, Window win, CameraController cam, Input input, Vec2f pos, Vec2f size, UnsafeGraphic graphic, Action onInteract)
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
        Vec2f camToObj = pos - cam.pos;
        float sqrDist = camToObj.sqrLength;
        if(sqrDist > SqrMaxInteractionDist)
            return;

        float dot = Vec2f.Dot(camToObj.normalized, cam.camera.forward);
        float minDot = Utils.Map(sqrDist, 0f, SqrMaxInteractionDist, MinDotAtMinDist, MinDotAtMaxDist);

        Out(Log.Debug, $"Dot: {dot}, minDot: {minDot}, sqrDist: {sqrDist}, SqrMaxInteractionDist: {SqrMaxInteractionDist}");

        if(dot < minDot)
            return;

        if(input.KeyDown(InputAction.Interact))
            onInteract();
    }
}