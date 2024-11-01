namespace Backrooms.Coroutines;

public interface ICoroutineInstruction
{
    bool KeepWaiting(float dt);
}