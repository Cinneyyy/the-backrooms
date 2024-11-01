using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;

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

        properties =
            type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<DontSerializeAttribute>() is null)
            .Where(p => p.PropertyType != typeof(object))
            .Where(p => p.CanWrite && p.CanRead)
            .Where(p => p.GetIndexParameters() is [])
            .ToArray();
        fields =
            type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.GetCustomAttribute<DontSerializeAttribute>() is null)
            .Where(f => f.FieldType != typeof(object))
            .ToArray();

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

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append('[');

        void stringifyMember(string name, Type type, object value, bool isLast)
        {
            sb.Append(name);
            sb.Append(": ");

            if(type == typeof(string))
                sb.Append($"\"{value as string}\"");
            else if(type.IsArray)
            {
                sb.Append('{');

                Array arrInstance = value as Array;
                int i = 0;
                foreach(object o in arrInstance)
                {
                    sb.Append(o.ToString());
                    if(i < arrInstance.Length - 1)
                        sb.Append(", ");
                }

                sb.Append('}');
            }
            else
                sb.Append(value.ToString());

            if(!isLast)
                sb.Append(", ");
        }

        foreach(PropertyInfo info in properties)
            stringifyMember(info.Name, info.PropertyType, info.GetValue(this), info == properties[^1]);
        foreach(FieldInfo info in fields)
            stringifyMember(info.Name, info.FieldType, info.GetValue(this), info == fields[^1]);

        sb.Append(']');
        return sb.ToString();
    }


    public static bool IsProperty(int index) => index < properties.Length;
}