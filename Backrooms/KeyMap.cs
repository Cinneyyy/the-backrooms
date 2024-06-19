using System;
using System.Collections;
using System.Windows.Forms;

namespace Backrooms;

public class KeyMap(Input input) : IEnumerable
{
    public readonly Input input = input;
    public readonly Keys[] bindings = new Keys[Enum.GetValues<GameKey>().Length];


    public Keys this[GameKey gameKey]
    {
        get => GetKey(gameKey);
        set => SetKey(gameKey, value);
    }

    public Keys GetKey(GameKey key)
        => bindings[(byte)key];

    public Keys SetKey(GameKey key, Keys newBinding)
        => bindings[(byte)key] = newBinding;

    public bool KeyHelt(GameKey key) => input.KeyHelt(this[key]);
    public bool KeyDown(GameKey key) => input.KeyDown(this[key]);
    public bool KeyUp(GameKey key) => input.KeyUp(this[key]);

    public IEnumerator GetEnumerator() => bindings.GetEnumerator();

    public void Add(GameKey key, Keys binding)
        => SetKey(key, binding);
}