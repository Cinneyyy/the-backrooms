using System.Windows.Forms;

namespace Backrooms.InputSystem;

public class InputGetter(Input input)
{
    private readonly Input input = input;
    private InputLock? inputLock;


    public bool LockMatches(InputLock? @lock) => inputLock is null || @lock is not null && inputLock.Value == @lock.Value;

    public void Lock(InputLock @lock) => inputLock = @lock;
    public void Unlock() => inputLock = null;

    public bool KeyDown(Keys key, InputLock? inputLock = null) => LockMatches(inputLock) && input.KeyDown(key);
    public bool KeyDown(InputAction action, InputLock? inputLock = null) => LockMatches(inputLock) && input.KeyDown(action);

    public bool KeyUp(Keys key, InputLock? inputLock = null) => LockMatches(inputLock) && input.KeyUp(key);
    public bool KeyUp(InputAction action, InputLock? inputLock = null) => LockMatches(inputLock) && input.KeyUp(action);

    public bool KeyHelt(Keys key, InputLock? inputLock = null) => LockMatches(inputLock) && input.KeyHelt(key);
    public bool KeyHelt(InputAction action, InputLock? inputLock = null) => LockMatches(inputLock) && input.KeyHelt(action);
}