using System;
using System.IO;
using System.Reflection;

namespace Backrooms.Serialization;

/// <summary>
/// Format:
/// [length]
/// {
///     [property index]
///     [length, if data is custom type]
///     { property data }
/// } (n times, properties before fields)
/// </summary>
public static class BinarySerializer<T> where T : Serializable<T>, new()
{
    public static byte[] Serialize(T instance)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write(0);

        for(int i = 0; i < Serializable<T>.properties.Length; i++)
        {
            PropertyInfo info = Serializable<T>.properties[i];
            WriteSerializedTypeData(info.PropertyType, info.GetValue(instance), writer, i);
        }

        for(int i = 0; i < Serializable<T>.fields.Length; i++)
        {
            FieldInfo info = Serializable<T>.fields[i];
            WriteSerializedTypeData(info.FieldType, info.GetValue(instance), writer, i + Serializable<T>.properties.Length);
        }

        int length = (int)stream.Position;
        stream.Position = 0;
        writer.Write(length);

        return stream.ToArray();
    }

    public static T Deserialize(byte[] data)
    {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        int length = reader.ReadInt32();
        T target = new();

        while(stream.Position < length)
        {
            int index = reader.ReadInt32();

            if(Serializable<T>.IsProperty(index))
            {
                PropertyInfo info = Serializable<T>.properties[index];
                ReadSerializedTypeData(info.PropertyType, info.Name, ref target, reader);
            }
            else
            {
                FieldInfo info = Serializable<T>.fields[index - Serializable<T>.properties.Length];
                ReadSerializedTypeData(info.FieldType, info.Name, ref target, reader);
            }
        }

        return target;
    }


    private static void WriteSerializedTypeData(Type type, object value, BinaryWriter writer, int index)
    {
        if(type.IsSubclassOfGeneric(typeof(Serializable<>)))
        {
            writer.Write(index);

            byte[] data = 
                typeof(BinarySerializer<>)
                .MakeGenericType(type)
                .GetMethod("Serialize")
                .Invoke(null, [value])
                as byte[];

            writer.Write(data.Length);
            writer.Write(data);
        }
        else
            WriteSerializedPrimitive(type, value, writer, index);

    }

    private static void ReadSerializedTypeData(Type type, string memName, ref T target, BinaryReader reader)
    {
        if(type.IsSubclassOfGeneric(typeof(Serializable<>)))
        {
            int length = reader.ReadInt32();
            byte[] data = reader.ReadBytes(length);

            object deserializedMember =
                typeof(BinarySerializer<>)
                .MakeGenericType(type)
                .GetMethod("Deserialize")
                .Invoke(null, [data]);

            target.SetMember(memName, deserializedMember);
        }
        else
            ReadSerializedPrimitive(type, memName, ref target, reader);
    }

    private static void WriteSerializedPrimitive(Type type, object value, BinaryWriter writer, int index)
    {
        writer.Write(index);

        switch(Type.GetTypeCode(type))
        {
            case TypeCode.Byte: writer.Write((byte)value); break;
            case TypeCode.SByte: writer.Write((sbyte)value); break;
            case TypeCode.Int16: writer.Write((short)value); break;
            case TypeCode.UInt16: writer.Write((ushort)value); break;
            case TypeCode.Int32: writer.Write((int)value); break;
            case TypeCode.UInt32: writer.Write((uint)value); break;
            case TypeCode.Int64: writer.Write((long)value); break;
            case TypeCode.UInt64: writer.Write((ulong)value); break;
            case TypeCode.Single: writer.Write((float)value); break;
            case TypeCode.Double: writer.Write((double)value); break;
            case TypeCode.Decimal: writer.Write((decimal)value); break;
            case TypeCode.Boolean: writer.Write((bool)value); break;
            case TypeCode.String: writer.Write((string)value); break;
            case TypeCode.Char: writer.Write((char)value); break;
            case var _ when type == typeof(Vec2f): writer.Write((Vec2f)value); break;
            case var _ when type == typeof(Vec2i): writer.Write((Vec2i)value); break;
            default: throw new InvalidCastException($"Tried serializing non-primitive and non-serializable type '{type.FullName}'");
        }
    }
    
    private static void ReadSerializedPrimitive(Type type, string memName, ref T target, BinaryReader reader)
        => target.SetMember(memName, Type.GetTypeCode(type) switch {
            TypeCode.Byte => reader.ReadByte(),
            TypeCode.SByte => reader.ReadSByte(),
            TypeCode.Int16 => reader.ReadInt16(),
            TypeCode.UInt16 => reader.ReadUInt16(),
            TypeCode.Int32 => reader.ReadInt32(),
            TypeCode.UInt32 => reader.ReadUInt32(),
            TypeCode.Int64 => reader.ReadInt64(),
            TypeCode.UInt64 => reader.ReadUInt64(),
            TypeCode.Single => reader.ReadSingle(),
            TypeCode.Double => reader.ReadDouble(),
            TypeCode.Decimal => reader.ReadDecimal(),
            TypeCode.Boolean => reader.ReadBoolean(),
            TypeCode.String => reader.ReadString(),
            TypeCode.Char => reader.ReadChar(),
            _ when type == typeof(Vec2f) => reader.ReadVec2f(),
            _ when type == typeof(Vec2i) => reader.ReadVec2i(),
            _ => throw new InvalidCastException($"Tried serializing non-primitive and non-serializable type '{type.FullName}'")
        });
}