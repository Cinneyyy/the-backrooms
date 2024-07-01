﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Backrooms.Serialization;

public abstract class Serializable<TSelf>() where TSelf : Serializable<TSelf>, new()
{
    public static readonly PropertyInfo[] properties;
    public static readonly FieldInfo[] fields;
    public static readonly Dictionary<string, int> memberIndices = [];
    public static readonly int memberCount;


    static Serializable()
    {
        Type type = typeof(TSelf);

        properties = (from p in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                      where p.GetCustomAttribute<DontSerializeAttribute>() is null
                      where p.PropertyType != typeof(object)
                      where p.CanWrite && p.CanRead
                      where p.GetIndexParameters() is []
                      select p).ToArray();
        fields = (from f in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                  where f.GetCustomAttribute<DontSerializeAttribute>() is null
                  where f.FieldType != typeof(object)
                  select f).ToArray();

        memberCount = properties.Length + fields.Length;

        for(int i = 0; i < properties.Length; i++)
            memberIndices[properties[i].Name] = i;
        for(int i = 0; i < fields.Length; i++)
            memberIndices[fields[i].Name] = i + properties.Length;
    }


    public void SetMember(int index, object value)
    {
        if(IsProperty(index))
            properties[index].SetValue(this, value);
        else
            fields[index - properties.Length].SetValue(this, value);

    }
    public void SetMember(string name, object value) => SetMember(memberIndices[name], value);

    public object GetMember(int index)
    {
        if(IsProperty(index))
            return properties[index].GetValue(this);
        else
            return fields[index - properties.Length].GetValue(this);
    }
    public object GetMember(string name) => GetMember(memberIndices[name]);


    public static bool IsProperty(int index) => index < properties.Length;
}