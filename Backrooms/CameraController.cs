using Backrooms.Online;

namespace Backrooms;

public class CameraController
{
    public const float HITBOX_RADIUS = .25f;

    public readonly Camera camera;
    public readonly MpHandler mpHandler;
    public readonly Input input;
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


    public CameraController(Camera camera, MpHandler mpHandler, Window window, Input input, Map map, Renderer renderer)
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
            (camera.right * (input.KeyHelt(GameKey.MoveLeft) ? 1f : input.KeyHelt(GameKey.MoveRight) ? -1f : 0f)
            + camera.forward * (input.KeyHelt(GameKey.MoveBackward) ? -1f : input.KeyHelt(GameKey.MoveForward) ? 1f : 0f))
            .normalized;

        Vec2f currPos = pos;
        Vec2f newPos = pos + moveSpeed * dt * move;

        pos = map.ResolveIntersectionIfNecessery(currPos, newPos, HITBOX_RADIUS, out _);

        if(input.lockCursor)
            camera.angle += input.mouseDelta.x * renderer.singleDownscaleFactor * sensitivity / dt;
    }
}