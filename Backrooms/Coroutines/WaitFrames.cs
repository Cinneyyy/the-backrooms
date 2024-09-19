namespace Backrooms.Coroutines;

public class WaitFrames(int frameCount) : ICoroutineInstruction
{
    public bool KeepWaiting(float dt)
        => --frameCount > 0;
}