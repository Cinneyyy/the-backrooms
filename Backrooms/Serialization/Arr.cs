using System;
using System.Collections;
using System.Collections.Generic;

namespace Backrooms.Serialization;

public sealed class Arr<T>(ArrElem<T>[] elems) : Serializable<Arr<T>>, IEnumerable<ArrElem<T>>, IEnumerable<T>
{
    public ArrElem<T>[] elems = elems;


    public Arr(int length) : this(new ArrElem<T>[length]) { }

    public Arr(int length, ArrElem<T> defaultValue) : this(new ArrElem<T>[length])
        => Array.Fill(elems, defaultValue);

    public Arr() : this(null) { }


    public T this[int index]
    {
        get => elems[index];
        set => elems[index].value = value;
    }


    public override string ToString() => $"{{{elems.FormatStr(", ")}}}";

    public void Add(ArrElem<T> elem) => elems = [..(elems ?? []), elem];

    public IEnumerator<ArrElem<T>> GetEnumerator() => ((IEnumerable<ArrElem<T>>)elems).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => elems.GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        foreach(ArrElem<T> elem in this)
            yield return (T)elem;
    }


    public static implicit operator Arr<T>(T[] values) => new(Array.ConvertAll(values, t => (ArrElem<T>)t));
}