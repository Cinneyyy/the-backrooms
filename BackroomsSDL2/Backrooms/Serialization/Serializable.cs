using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Backrooms.Serialization;

public abstract class Serializable<TSelf> where TSelf : Serializable<TSelf>
{
    static Serializable()
    {
        type = typeof(TSelf);

        fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<DontSerializeAttribute>() is null)
            .ToArray();

        properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<DontSerializeAttribute>() is null)
            .Where(m => m.CanWrite && m.CanRead)
            .Where(m => m.GetIndexParameters() is [])
            .ToArray();

        fieldCount = fields.Length;
        propertyCount = properties.Length;
        memberCount = fieldCount + propertyCount;

        for(int i = 0; i < fieldCount; i++)
            memberIndices[fields[i].Name] = i;
        for(int i = 0; i < propertyCount; i++)
            memberIndices[properties[i].Name] = i + fieldCount;
    }


    public static readonly Type type;
    public static readonly FieldInfo[] fields;
    public static readonly PropertyInfo[] properties;
    public static readonly Dictionary<string, int> memberIndices = [];
    public static readonly int fieldCount, propertyCount, memberCount;


    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append('[');

        void stringify(string name, Type type, object value, bool isLast)
        {
            sb.Append(name);
            sb.Append(": ");

            if(type == typeof(string))
            {
                sb.Append('"');
                sb.Append(value as string);
                sb.Append('"');
            }
            else if(type.IsArray)
            {
                sb.Append('{');

                Array arr = value as Array;

                for(int i = 0; i < arr.Length; i++)
                {
                    sb.Append(arr.GetValue(0).ToString());
                    if(i != arr.Length - 1)
                        sb.Append(", ");
                }
            }
            else
                sb.Append(value.ToString());

            if(!isLast)
                sb.Append(", ");
        }

        foreach(FieldInfo info in fields)
            stringify(info.Name, info.FieldType, info.GetValue(this), info == fields[^1]);
        foreach(PropertyInfo info in properties)
            stringify(info.Name, info.PropertyType, info.GetValue(this), info == properties[^1]);

        sb.Append(']');
        return sb.ToString();
    }

    public void SetMember(int index, object value)
    {
        if(IsField(index))
            fields[index].SetValue(this, value);
        else
            properties[index - fieldCount].SetValue(this, value);
    }
    public void SetMember(string name, object value)
        => SetMember(memberIndices[name], value);

    public object GetMember(int index)
    {
        if(IsField(index))
            return fields[index].GetValue(this);
        else
            return properties[index - fieldCount].GetValue(this);
    }
    public object GetMember(string name)
        => GetMember(memberIndices[name]);
    public T GetMember<T>(int index)
        => (T)GetMember(index);
    public T GetMember<T>(string name)
        => GetMember<T>(memberIndices[name]);


    public static bool IsField(int index) => index < fieldCount;
}