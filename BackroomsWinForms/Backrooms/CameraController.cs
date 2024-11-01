namespace Backrooms;

public class CameraController
{
    public const float HITBOX_RADIUS = .25f;

    public readonly Camera camera;
    public readonly MpManager mpManager;
    public readonly Input input;
    public readonly Map map;
    public readonly Renderer renderer;
    public float walkSpeed = 1.5f, sprintSpeed = 2.75f, sensitivity = 1/5000f;
    public bool canMove = false, noClip = false;


    public Vec2f pos
    {
        get => camera.pos;
        set {
            camera.pos = value;

            if(mpManager.isConnected)
                SendClientPosition();
        }
    }


    public CameraController(Camera camera, MpManager mpManager, Window window, Input input, Map map, Renderer renderer)
    {
        this.camera = camera;
        this.mpManager = mpManager;
        this.input = input;
        this.map = map;
        this.renderer = renderer;

        window.tick += Tick;
    }


    public void SendClientPosition()
    {
        mpManager.clientState.pos = pos;
        mpManager.SyncClientState("pos");
    }


    private void Tick(float dt)
    {
        if(!canMove)
            return;

        Vec2f move =
            (camera.right * Utils.ToTernary(input, InputAction.MoveRight, InputAction.MoveLeft))
            + (camera.forward * Utils.ToTernary(input, InputAction.MoveBackward, InputAction.MoveForward))
            .normalized;

        float moveSpeed = input.KeyHelt(InputAction.Sprint) ? sprintSpeed : walkSpeed;
        Vec2f currPos = pos;
        Vec2f newPos = pos + moveSpeed * dt * move;

        pos = noClip ? newPos : map.ResolveIntersectionIfNecessery(currPos, newPos, HITBOX_RADIUS, out _);

        if(input.lockCursor)
            camera.angle += input.mouseDelta.x * renderer.singleDownscaleFactor * sensitivity / dt;
    }
}