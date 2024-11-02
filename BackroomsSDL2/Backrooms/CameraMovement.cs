using Backrooms.Extensions;

namespace Backrooms;

#pragma warning disable CA2211 // Non-constant fields should not be visible
public static class CameraMovement
{
    public static float hitRadius = .25f;
    public static bool canMove = true;
    public static float walkSpeed = 1.5f, runSpeed = 2.8f;
    public static float sensitivity = 6.75f;
    public static bool noClip;


    public static Vec2f pos
    {
        get => Camera.pos;
        set => Camera.pos = value;
    }
    public static float posX
    {
        get => Camera.pos.x;
        set => Camera.pos.x = value;
    }
    public static float posY
    {
        get => Camera.pos.y;
        set => Camera.pos.y = value;
    }
    public static float angle
    {
        get => Camera.angle;
        set => Camera.angle = value;
    }


    public static void Init()
        => Window.tickDt += Tick;


    private static void Tick(float dt)
    {
        if(!canMove)
            return;

        Vec2i input = new(
            Input.KeyHelt(Key.D).ToInt() - Input.KeyHelt(Key.A).ToInt(),
            Input.KeyHelt(Key.W).ToInt() - Input.KeyHelt(Key.S).ToInt());

        Vec2f move = (input.x * Camera.right + input.y * Camera.forward).normalized;

        float moveSpeed = Input.KeyHelt(Key.LShift) ? runSpeed : walkSpeed;
        Vec2f newPos = pos + moveSpeed * dt * move;

        pos = noClip ? newPos : Map.current.ResolveIntersectionIfNecessery(pos, newPos, hitRadius, out _);

        angle += sensitivity * Input.mouseDelta.x; // ANGLEFLIP
    }
}