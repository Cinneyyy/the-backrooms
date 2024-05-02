using System.IO;
using System;

namespace Backrooms.Online.Generic;

public abstract class State<TKey> : IState<TKey> where TKey : Enum
{
    public byte[] Serialize(params TKey[] dataKeys)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        PreSerialize(writer);

        foreach(TKey key in dataKeys)
        {
            writer.Write(ByteFromKey(key));
            SerializeField(writer, key);
        }

        PostSerialize(writer);

        return stream.ToArray();
    }

    public void Deserialize(byte[] data, int start, int? length, out int bytesRead)
    {
        try
        {
            using MemoryStream stream = new(data, start, length ?? data.Length - start);
            using BinaryReader reader = new(stream);

            PreDeserialize(reader);

            while(stream.Position < stream.Length)
            {
                byte next = reader.ReadByte();

                if(next == (byte)PacketType.EndOfData)
                    break;

                DeserializeField(reader, KeyFromByte(next));
            }

            PostDeserialize(reader);

            bytesRead = (int)stream.Position;
        }
        catch
        {
            Out(data.FormatStr(" ", b => Convert.ToString(b, 16).PadLeft(2, '0')));
            System.Diagnostics.Debugger.Break();
            throw;
        }
    }


    protected abstract void SerializeField(BinaryWriter writer, TKey key);
    protected abstract void DeserializeField(BinaryReader reader, TKey key);

    protected virtual void PreSerialize(BinaryWriter writer) { }
    protected virtual void PreDeserialize(BinaryReader reader) { }

    protected virtual void PostSerialize(BinaryWriter writer) { }
    protected virtual void PostDeserialize(BinaryReader reader) { }

    protected abstract TKey KeyFromByte(byte value);
    protected abstract byte ByteFromKey(TKey key);
}