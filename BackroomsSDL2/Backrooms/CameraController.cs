using Backrooms.Extensions;

namespace Backrooms;

public class CameraController
{
    public CameraController(Camera cam)
    {
        this.cam = cam;
        Window.tickDt += Tick;
    }


    public Camera cam;
    public float hitRadius = .25f;
    public bool canMove = true;
    public float walkSpeed = 1.5f, runSpeed = 2.8f;
    public float sensitivity = 5f;
    public bool noClip;


    public Vec2f pos
    {
        get => cam.pos;
        set => cam.pos = value;
    }
    public float posX
    {
        get => cam.pos.x;
        set => cam.pos.x = value;
    }
    public float posY
    {
        get => cam.pos.y;
        set => cam.pos.y = value;
    }
    public float angle
    {
        get => cam.angle;
        set => cam.angle = value;
    }


    private void Tick(float dt)
    {
        if(!canMove)
            return;

        Vec2i input = new(
            Input.KeyHelt(Key.D).ToInt() - Input.KeyHelt(Key.A).ToInt(),
            Input.KeyHelt(Key.W).ToInt() - Input.KeyHelt(Key.S).ToInt());

        Vec2f move = (input.x * cam.right + input.y * cam.forward).normalized;

        float moveSpeed = Input.KeyHelt(Key.LShift) ? runSpeed : walkSpeed;
        Vec2f newPos = pos + moveSpeed * dt * move;

        pos = noClip ? newPos : Raycaster.map.ResolveIntersectionIfNecessery(pos, newPos, hitRadius, out _);

        angle += sensitivity * Input.mouseDelta.x; // ANGLEFLIP
    }
}