using System;
using System.IO;

namespace Backrooms.Serialization;

/// <summary>Supports all number types, string, bool, char, Vec2f, Vec2i, and enums</summary>
public static class PrimitiveSerializer
{
    public static void Serialize(Type type, object value, BinaryWriter writer)
    {
        switch(Type.GetTypeCode(type.IsEnum ? type.GetEnumUnderlyingType() : type))
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
            default: throw new InvalidCastException($"Tried serializing non-primitive '{type.FullName}'");
        }
    }
    public static byte[] Serialize(Type type, object value)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);
        Serialize(type, value, writer);
        return stream.ToArray();
    }
    public static void Serialize<T>(T value, BinaryWriter writer)
        => Serialize(typeof(T), value, writer);
    public static byte[] Serialize<T>(T value)
        => Serialize(typeof(T), value);

    public static object Deserialize(Type type, BinaryReader reader)
        => Type.GetTypeCode(type.IsEnum ? type.GetEnumUnderlyingType() : type) switch {
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
            _ => throw new InvalidCastException($"Tried deserializing non-primitive type '{type.FullName}'")
        };
    public static object Deserialize(Type type, byte[] bytes)
    {
        using MemoryStream stream = new(bytes);
        using BinaryReader reader = new(stream);
        return Deserialize(type, reader);
    }
    public static T Deserialize<T>(BinaryReader reader)
        => (T)Deserialize(typeof(T), reader);
    public static T Deserialize<T>(byte[] bytes)
        => (T)Deserialize(typeof(T), bytes);

}