using System;
using System.IO;

namespace Backrooms.Online;

public struct ServerState : IState
{
    public Vec2f olafPos;
    public int levelSeed;


    void IState.Deserialize(byte[] data, int length)
    {
        using MemoryStream stream = new(data, 0, length);
        using BinaryReader reader = new(stream);

        while(reader.BaseStream.Position < length)
        {
            byte next = reader.ReadByte();

            switch((ByteKey)next)
            {
                case ByteKey.S_LevelSeed: levelSeed = reader.ReadInt32(); break;
                case ByteKey.S_OlafPos: olafPos = new(reader.ReadSingle(), reader.ReadSingle()); break;
                default: 
                    Console.WriteLine($"Unrecognized byte key encountered in ServerState.Deserialize(): 0x{Convert.ToString(next, 16)}");
                    reader.BaseStream.Position = reader.BaseStream.Length; 
                    break;
            }
        }
    }

    readonly byte[] IState.Serialize(byte[] fieldKeys)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        foreach(byte keyByte in fieldKeys)
        {
            ByteKey key = (ByteKey)keyByte;
            writer.Write(keyByte);

            switch(key)
            {
                case ByteKey.S_OlafPos:
                    writer.Write(olafPos.x);
                    writer.Write(olafPos.y);
                    break;
                case ByteKey.S_LevelSeed:
                    writer.Write(levelSeed);
                    break;
            }
        }

        writer.Write((byte)0);
        return stream.ToArray();
    }
}