using System.Collections;

namespace Backrooms.Coroutines;

public class Coroutine
{
    public readonly IEnumerator iterator;
    public bool cancelled;

    private ICoroutineInstruction instruction;
    private readonly Window win;


    public bool isFinished { get; private set; }


    public Coroutine(Window win, IEnumerator iterator)
    {
        this.iterator = iterator;
        this.win = win;
        win.tick += Tick;
    }


    public void Tick(float dt)
    {
        if(this.instruction is not null)
            if(!this.instruction.KeepWaiting(dt))
                this.instruction = null;
            else
                return;

        if(cancelled || !iterator.MoveNext())
        {
            win.tick -= Tick;
            isFinished = true;
            return;
        }

        if(iterator.Current is not null and ICoroutineInstruction instruction)
            this.instruction = instruction;
    }

    public void Cancel()
    {
        cancelled = true;
        isFinished = true;
        win.tick -= Tick;
    }
}