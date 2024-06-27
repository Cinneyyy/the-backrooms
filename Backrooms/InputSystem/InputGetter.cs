using System.Windows.Forms;

namespace Backrooms.InputSystem;

public class InputGetter(Input input)
{
    private InputLock? inputLock;


    public Input unlockedInput { get; } = input;


    public bool LockMatches(InputLock? inputLock) => this.inputLock is null || inputLock is not null && this.inputLock.Value == inputLock.Value;

    public void Lock(InputLock inputLock) => this.inputLock = inputLock;
    public void Unlock() => inputLock = null;
    public void SetLock(InputLock inputLock, bool locked)
    {
        if(locked)
            Lock(inputLock);
        else
            Unlock();
    }

    public bool KeyDown(Keys key, InputLock? inputLock = null) => LockMatches(inputLock) && unlockedInput.KeyDown(key);
    public bool KeyDown(InputAction action, InputLock? inputLock = null) => LockMatches(inputLock) && unlockedInput.KeyDown(action);

    public bool KeyUp(Keys key, InputLock? inputLock = null) => LockMatches(inputLock) && unlockedInput.KeyUp(key);
    public bool KeyUp(InputAction action, InputLock? inputLock = null) => LockMatches(inputLock) && unlockedInput.KeyUp(action);

    public bool KeyHelt(Keys key, InputLock? inputLock = null) => LockMatches(inputLock) && unlockedInput.KeyHelt(key);
    public bool KeyHelt(InputAction action, InputLock? inputLock = null) => LockMatches(inputLock) && unlockedInput.KeyHelt(action);
}