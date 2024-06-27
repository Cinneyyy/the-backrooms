namespace Backrooms.InputSystem;

public readonly struct InputLock()
{
    public static readonly InputLock noLock = new() { value = 0 };
    public static readonly InputLock adminLock = new() { value = int.MaxValue };


    public int value { get; private init; } = RNG.signedInt;


    public override bool Equals(object obj) => obj is InputLock @lock && @lock == this;

    public override int GetHashCode() => value.GetHashCode();


    public static bool operator ==(InputLock a, InputLock b) => a == adminLock || b == adminLock || a.value == b.value;
    public static bool operator !=(InputLock a, InputLock b) => !(a == b);
}