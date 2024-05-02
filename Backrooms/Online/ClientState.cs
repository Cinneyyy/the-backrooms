using System.IO;
using System;

namespace Backrooms.Online;

public class ClientState : IState<StateKey>
{
    public Vec2f pos;
    public float rot;

    public static readonly StateKey[] allKeys = [StateKey.C_Pos, StateKey.C_Rot];


    public void Deserialize(byte[] data, int start, int end)
    {
        using MemoryStream stream = new(data, start, end - start);
        using BinaryReader reader = new(stream);

        reader.BaseStream.Position += 2;

        while(reader.BaseStream.Position < reader.BaseStream.Length)
        {
            byte next = reader.ReadByte();

            if(next == 0)
                break;

            switch((StateKey)next)
            {
                case StateKey.C_Pos:
                    pos = reader.ReadVec2f();
                    break;
                case StateKey.C_Rot:
                    rot = reader.ReadSingle();
                    break;
                default:
                    Console.WriteLine($"Unrecognized byte key encountered in ServerState.Deserialize(): 0x{Convert.ToString(next, 16)}");
                    reader.BaseStream.Position = reader.BaseStream.Length;
                    break;
            }
        }
    }

    /// <summary>
    /// Serializes client data into an array of bytes: ([StateKey.Client][clientId]) when client != null; (..[StateKey][state data]) x times
    /// </summary>
    public byte[] Serialize(byte? clientId = null, params StateKey[] dataKeys)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        if(clientId is not null)
        {
            writer.Write((byte)StateKey.Client);
            writer.Write(clientId is not null);
        }

        foreach(StateKey key in dataKeys)
        {
            byte keyByte = (byte)key;
            writer.Write(keyByte);

            switch(key)
            {
                case StateKey.C_Pos:
                    writer.Write(pos);
                    break;
                case StateKey.C_Rot:
                    writer.Write(rot);
                    break;
            }
        }

        writer.Write((byte)0);
        return stream.ToArray();
    }
}