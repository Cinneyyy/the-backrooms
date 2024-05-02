using System.IO;
using System;

namespace Backrooms.Online;

public class ServerState : IState<StateKey>
{
    public int levelSeed;
    public Vec2f olafPos;
    public byte olafTarget;

    public static readonly StateKey[] allKeys = [StateKey.S_OlafPos, StateKey.S_LevelSeed];


    public void Deserialize(byte[] data, int start, int end)
    {
        using MemoryStream stream = new(data, start, end - start);
        using BinaryReader reader = new(stream);

        while(reader.BaseStream.Position < reader.BaseStream.Length)
        {
            byte next = reader.ReadByte();

            if(next == 0)
                break;

            switch((StateKey)next)
            {
                case StateKey.S_LevelSeed:
                    levelSeed = reader.ReadInt32();
                    break;
                case StateKey.S_OlafPos:
                    olafPos = reader.ReadVec2f();
                    break;
                case StateKey.S_OlafTarget:
                    olafTarget = reader.ReadByte();
                    break;
                default:
                    Out($"Unrecognized byte key encountered in ServerState.Deserialize(): 0x{Convert.ToString(next, 16)}");
                    reader.BaseStream.Position = reader.BaseStream.Length;
                    break;
            }
        }
    }

    public byte[] Serialize(byte? clientData = null, params StateKey[] dataKeys)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        Assert(clientData is null, "ServerState does not take in clientData!");

        foreach(StateKey key in dataKeys)
        {
            byte keyByte = (byte)key;
            writer.Write(keyByte);

            switch(key)
            {
                case StateKey.S_LevelSeed: writer.Write(levelSeed); break;
                case StateKey.S_OlafPos: writer.Write(olafPos); break;
                case StateKey.S_OlafTarget: writer.Write(olafTarget); break;
            }
        }

        writer.Write((byte)0);
        return stream.ToArray();
    }
}