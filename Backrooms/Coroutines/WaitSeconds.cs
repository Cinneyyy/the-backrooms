namespace Backrooms.Coroutines;

public class WaitSeconds(float seconds) : ICoroutineInstruction
{
    private float timePassed;
    private readonly float totalSeconds = seconds;


    public bool KeepWaiting(float dt)
    {
        timePassed += dt;
        return timePassed < totalSeconds;
    }
}