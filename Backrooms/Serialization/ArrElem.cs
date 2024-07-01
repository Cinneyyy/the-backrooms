namespace Backrooms.Serialization;

public sealed class ArrElem<T>(T value) : Serializable<ArrElem<T>>
{
    public T value = value;


    public ArrElem() : this(default(T)) { }


    public override string ToString() => value.ToString();


    public static implicit operator ArrElem<T>(T t) => new(t);
    public static implicit operator T(ArrElem<T> e) => e.value;
}