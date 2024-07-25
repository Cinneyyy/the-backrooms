using System;
using System.Collections;

namespace Backrooms.Coroutines;

public class Coroutine
{
    public readonly IEnumerator iterator;
    public bool cancelled;

    private readonly Window win;
    private ICoroutineInstruction currInstruction;
    private Coroutine currSubRoutine;


    public bool isFinished { get; private set; }


    public Coroutine(Window win, IEnumerator iterator)
    {
        this.iterator = iterator;
        this.win = win;
        win.tick += Tick;
    }


    public void Tick(float dt)
    {
        if(currInstruction is not null)
        {
            if(!currInstruction.KeepWaiting(dt))
                currInstruction = null;
            else
                return;
        }
        else if(currSubRoutine is not null)
        {
            if(currSubRoutine.isFinished)
                currSubRoutine = null;
            else
                return;
        }

        if(cancelled || !iterator.MoveNext())
        {
            win.tick -= Tick;
            isFinished = true;
            return;
        }

        if(iterator.Current is ICoroutineInstruction instruction && instruction is not null)
            currInstruction = instruction;
        else if(iterator.Current is IEnumerator subRoutine && subRoutine is not null)
            currSubRoutine = subRoutine.StartCoroutine(win);
    }

    public void Cancel()
    {
        cancelled = true;
        isFinished = true;
        win.tick -= Tick;
    }


    public static IEnumerator ActionOverTime(float seconds, ActionOverTime.InterpolatedCallback interpolatedAction = null, ActionOverTime.DeltaTimeCallback deltaAction = null)
    {
        yield return new ActionOverTime(seconds, interpolatedAction, deltaAction);
    }

    public static IEnumerator DelayedAction(float seconds, Action action)
    {
        yield return new WaitSeconds(seconds);
        action();
    }
}