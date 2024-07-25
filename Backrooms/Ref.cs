namespace Backrooms;

public class Ref<T>(T value) where T : struct
{
    public T value = value;


    public Ref() : this(default(T)) { }


    public static implicit operator T(Ref<T> reference) => reference.value;
}