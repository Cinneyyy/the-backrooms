﻿namespace Backrooms;

public class Ref<T>(T value) where T : struct
{
    public T value = value;


    public Ref() : this(default(T)) { }


    public override string ToString() => $"(Reference to:) {value}";


    public static T operator -(Ref<T> tRef) => tRef.value;


    public static implicit operator T(Ref<T> reference) => reference.value;
}