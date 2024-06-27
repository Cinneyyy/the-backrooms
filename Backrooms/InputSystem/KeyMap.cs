using System;
using System.Collections;
using System.Windows.Forms;

namespace Backrooms.InputSystem;

public class KeyMap(Input input) : IEnumerable
{
    public readonly Input input = input;
    public readonly Keys[] bindings = new Keys[Enum.GetValues<InputAction>().Length];


    public Keys this[InputAction gameKey]
    {
        get => GetKey(gameKey);
        set => SetKey(gameKey, value);
    }

    public Keys GetKey(InputAction key)
        => bindings[(byte)key];

    public Keys SetKey(InputAction key, Keys newBinding)
        => bindings[(byte)key] = newBinding;

    public bool KeyHelt(InputAction key) => input.KeyHelt(this[key]);
    public bool KeyDown(InputAction key) => input.KeyDown(this[key]);
    public bool KeyUp(InputAction key) => input.KeyUp(this[key]);

    public IEnumerator GetEnumerator() => bindings.GetEnumerator();

    public void Add(InputAction key, Keys binding)
        => SetKey(key, binding);
}