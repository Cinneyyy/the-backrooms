using System.IO;
using System;

namespace Backrooms.Online;

public class ServerState : IState<DataKey>
{
    public Vec2f olafPos;
    public int levelSeed;


    void IState<DataKey>.Deserialize(byte[] data, int start, int length)
    {
        using MemoryStream stream = new(data, start, length);
        using BinaryReader reader = new(stream);

        while(reader.BaseStream.Position < length)
        {
            byte next = reader.ReadByte();

            if(next == 0)
                break;

            switch((DataKey)next)
            {
                case DataKey.S_LevelSeed:
                    levelSeed = reader.ReadInt32();
                    break;
                case DataKey.S_OlafPos:
                    olafPos = reader.ReadVec2f();
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
                case DataKey.S_OlafPos: writer.Write(olafPos); break;
                case DataKey.S_LevelSeed: writer.Write(levelSeed); break;
            }
        }

        writer.Write((byte)0);
        return stream.ToArray();
    }
}