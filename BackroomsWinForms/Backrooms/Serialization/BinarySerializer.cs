using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace Backrooms.Serialization;

/// <summary>
/// Format:
/// [length] :: 4 bytes
/// {
///     [property index] :: 4 bytes
///     [length, if data is custom type] :: 4 bytes
///     { property data } :: ? bytes
/// } (n times, properties before fields)
/// </summary>
public static class BinarySerializer<T> where T : Serializable<T>, new()
{
    public static byte[] Serialize(T instance, CompressionLevel compression = CompressionLevel.Optimal)
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

        if(compression == CompressionLevel.NoCompression)
            return stream.ToArray();

        using MemoryStream compressedStream = new();
        using(GZipStream gZipStream = new(compressedStream, compression, true))
        {
            stream.Position = 0;
            stream.CopyTo(gZipStream);
        }

        return compressedStream.ToArray();
    }
    public static byte[] SerializeMembers(T instance, string[] members, CompressionLevel compression = CompressionLevel.Optimal)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write(0);

        foreach(string mem in members)
        {
            int index = Serializable<T>.memberIndices[mem];

            if(Serializable<T>.IsProperty(index))
            {
                PropertyInfo info = Serializable<T>.properties[index];
                WriteSerializedTypeData(info.PropertyType, info.GetValue(instance), writer, index);
            }
            else
            {
                FieldInfo info = Serializable<T>.fields[index - Serializable<T>.properties.Length];
                WriteSerializedTypeData(info.FieldType, info.GetValue(instance), writer, index);
            }
        }

        int length = (int)stream.Position;
        stream.Position = 0;
        writer.Write(length);

        if(compression == CompressionLevel.NoCompression)
            return stream.ToArray();

        using MemoryStream compressedStream = new();
        using(GZipStream gZipStream = new(compressedStream, compression, true))
        {
            stream.Position = 0;
            stream.CopyTo(gZipStream);
        }

        return compressedStream.ToArray();
    }

    public static void DeserializeRef(byte[] data, ref T target, bool decompress = true)
    {
        MemoryStream stream;

        if(decompress)
        {
            stream = new();

            using MemoryStream compressedData = new(data);
            using GZipStream gZipStream = new(compressedData, CompressionMode.Decompress, true);

            gZipStream.CopyTo(stream);
            stream.Position = 0;
        }
        else
            stream = new(data) {
                Position = 0
            };

        using BinaryReader reader = new(stream);

        int length = reader.ReadInt32();
        target ??= new();

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

        stream.Dispose();
    }
    public static void DeserializeTo(byte[] data, out T target, bool decompress = true)
    {
        target = new();
        DeserializeRef(data, ref target, decompress);
    }
    public static T Deserialize(byte[] data, bool decompress = true)
    {
        DeserializeTo(data, out T target, decompress);
        return target;
    }


    private static void WriteSerializedTypeData(Type type, object value, BinaryWriter writer, int index)
    {
        writer.Write(index);

        if(type.IsSubclassOfGeneric(typeof(Serializable<>)))
        {
            byte[] data =
                typeof(BinarySerializer<>)
                .MakeGenericType(type)
                .GetMethod(nameof(Serialize))
                .Invoke(null, [value, CompressionLevel.NoCompression])
                as byte[];

            writer.Write(data.Length);
            writer.Write(data);
        }
        else if(type.IsArray)
        {
            Type baseType = type.GetElementType();
            if(!baseType.IsSubclassOfGeneric(typeof(Serializable<>)))
                throw new($"Cannot deserialize array elments, of which the element type does not derive from Serializable<TSelf>");

            MethodInfo serialize =
                typeof(BinarySerializer<>)
                .MakeGenericType(baseType)
                .GetMethod(nameof(Serialize));

            using MemoryStream arrStream = new();
            using BinaryWriter arrWriter = new(arrStream);

            Array arrValue = value as Array;
            for(int i = 0; i < arrValue.Length; i++)
            {
                byte[] elemData = serialize.Invoke(null, [arrValue.GetValue(i), CompressionLevel.NoCompression]) as byte[];
                arrWriter.Write(elemData.Length);
                arrWriter.Write(elemData);
            }

            byte[] arrData = arrStream.ToArray();
            writer.Write(arrValue.Length);
            writer.Write(arrData);
        }
        else
            PrimitiveSerializer.Serialize(type, value, writer);
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
                .GetMethod(nameof(Deserialize))
                .Invoke(null, [data, false]);

            target.SetMember(memName, deserializedMember);
        }
        else if(type.IsArray)
        {
            Type baseType = type.GetElementType();
            if(!baseType.IsGenericType || baseType.GetGenericTypeDefinition() != typeof(ArrElem<>))
                throw new($"Cannot serialize array elments which are not boxed in ArrElem<T> (current type: {baseType.Name})");

            int length = reader.ReadInt32();
            Array array = Array.CreateInstance(baseType, length);
            MethodInfo deserialize =
                typeof(BinarySerializer<>)
                .MakeGenericType(baseType)
                .GetMethod(nameof(Deserialize));

            for(int i = 0; i < length; i++)
            {
                int elemLength = reader.ReadInt32();
                byte[] data = reader.ReadBytes(elemLength);

                array.SetValue(deserialize.Invoke(null, [data, false]), i);
            }

            target.SetMember(memName, array);
        }
        else
            target.SetMember(memName, PrimitiveSerializer.Deserialize(type, reader));
    }
}