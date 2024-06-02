using System;

namespace Backrooms.Coroutines;

public class ActionOverTime(float seconds, ActionOverTime.InterpolatedCallback interpolatedAction = null, ActionOverTime.DeltaTimeCallback deltaAction = null) : ICoroutineInstruction
{
    public delegate void InterpolatedCallback(float t);
    public delegate void DeltaTimeCallback(float dt);


    private float secondsPassed;


    public bool KeepWaiting(float dt)
    {
        secondsPassed += dt;

        if(secondsPassed >= seconds)
        {
            interpolatedAction?.Invoke(1f);
            deltaAction?.Invoke(dt);
            return false;
        }

        interpolatedAction?.Invoke(secondsPassed / seconds);
        deltaAction?.Invoke(dt);
        return true;
    }
}