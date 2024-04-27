using System.IO;
using System;

namespace Backrooms.Online;

public class ClientState : IState<DataKey>
{
    public Vec2f pos;
    public float rot;


    void IState<DataKey>.Deserialize(byte[] data, int start, int length)
    {
        using MemoryStream stream = new(data, start, length);
        using BinaryReader reader = new(stream);

        while(reader.BaseStream.Position < length)
        {
            byte next = reader.ReadByte();

            switch((DataKey)next)
            {
                case DataKey.C_Pos:
                    pos = reader.ReadVec2f();
                    break;
                case DataKey.C_Rot:
                    rot = reader.ReadSingle();
                    break;
                default:
                    Console.WriteLine($"Unrecognized byte key encountered in ServerState.Deserialize(): 0x{Convert.ToString(next, 16)}");
                    reader.BaseStream.Position = reader.BaseStream.Length;
                    break;
            }
        }
    }

    byte[] IState<DataKey>.Serialize(DataKey[] dataKeys)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        foreach(DataKey key in dataKeys)
        {
            byte keyByte = (byte)key;
            writer.Write(keyByte);

            switch(key)
            {
                case DataKey.C_Pos:
                    writer.Write(pos);
                    break;
                case DataKey.C_Rot:
                    writer.Write(rot);
                    break;
            }
        }

        writer.Write(0);
        return stream.ToArray();
    }
}