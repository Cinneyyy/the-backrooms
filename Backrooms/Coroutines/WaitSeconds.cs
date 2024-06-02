namespace Backrooms.Coroutines;

public class WaitSeconds(float seconds) : ICoroutineInstruction
{
    private float timePassed;


    public bool KeepWaiting(float dt)
    {
        timePassed += dt;
        return timePassed < seconds;
    }
}