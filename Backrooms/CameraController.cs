using Backrooms.InputSystem;
using Backrooms.OnlineOld;

namespace Backrooms;

public class CameraController
{
    public const float HITBOX_RADIUS = .25f;

    public readonly Camera camera;
    public readonly MpHandler mpHandler;
    public readonly InputGetter input;
    public readonly Map map;
    public readonly Renderer renderer;
    public float moveSpeed = 2f, sensitivity = 1/5000f;
    public bool canMove = false;

    
    public Vec2f pos
    {
        get => camera.pos;
        set {
            camera.pos = value;
            
            if(mpHandler.ready)
                SendClientPosition();
        }
    }


    public CameraController(Camera camera, MpHandler mpHandler, Window window, InputGetter input, Map map, Renderer renderer)
    {
        this.camera = camera;
        this.mpHandler = mpHandler;
        this.input = input;
        this.map = map;
        this.renderer = renderer;

        mpHandler.onFinishConnect += SendClientPosition;

        window.tick += Tick;
    }


    public void SendClientPosition()
    {
        mpHandler.ownClientState.pos = pos;
        mpHandler.SendClientStateChange(StateKey.C_Pos);
    }

    
    private void Tick(float dt)
    {
        if(!canMove)
            return;

        Vec2f move = 
            (camera.right * (input.KeyHelt(InputAction.MoveLeft) ? 1f : input.KeyHelt(InputAction.MoveRight) ? -1f : 0f)
            + camera.forward * (input.KeyHelt(InputAction.MoveBackward) ? -1f : input.KeyHelt(InputAction.MoveForward) ? 1f : 0f))
            .normalized;

        Vec2f currPos = pos;
        Vec2f newPos = pos + moveSpeed * dt * move;

        pos = map.ResolveIntersectionIfNecessery(currPos, newPos, HITBOX_RADIUS, out _);

        if(input.unlockedInput.lockCursor)
            camera.angle += input.unlockedInput.mouseDelta.x * renderer.singleDownscaleFactor * sensitivity / dt;
    }
}