namespace Backrooms.Coroutines;

public class WaitFrame : ICoroutineInstruction
{
    public bool KeepWaiting(float dt)
        => false;
}