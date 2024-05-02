using System.IO;
using System;
using Backrooms.Online.Generic;
using System.Linq;

namespace Backrooms.Online;

/// <summary>
/// Layout of serialized ClientState ([..] == 1 byte, {..} == x times): <br />
/// <code>
/// [PacketType.ClientState as byte]
/// [Client ID]
/// {
///     [StateKey as byte]
///     { [data] } (* size of data in bytes)
/// } (* amount of data keys)
/// [0xff] (== PacketType.EndOfData)
/// </code>
/// </summary>
public class ClientState(byte clientId) : State<StateKey>
{
    public byte clientId = clientId;
    public Vec2f pos;
    public float rot;

    public static readonly StateKey[] allKeys = (from v in Enum.GetValues<StateKey>()
                                                 where (v != StateKey.Client) && (((byte)v & 0b1100_0000) == (byte)StateKey.Client)
                                                 select v).ToArray();


    protected override StateKey KeyFromByte(byte value) => (StateKey)value;
    protected override byte ByteFromKey(StateKey key) => (byte)key;

    protected override void SerializeField(BinaryWriter writer, StateKey key)
    {
        switch(key)
        {
            case StateKey.C_ClientId: writer.Write(clientId); break;
            case StateKey.C_Pos: writer.Write(pos); break;
            case StateKey.C_Rot: writer.Write(rot); break;
            default: throw new($"Invalid StateKey in ClientState.SerializeField(): {key} // {(byte)key}");
        }
    }

    protected override void DeserializeField(BinaryReader reader, StateKey key)
    {
        switch(key)
        {
            case StateKey.C_ClientId: clientId = reader.ReadByte(); break;
            case StateKey.C_Pos: pos = reader.ReadVec2f(); break;
            case StateKey.C_Rot: rot = reader.ReadSingle(); break;
            default: break;//throw new($"Invalid StateKey in ClientState.DeserializeField(): {key} // {(byte)key}");
        }
    }

    protected override void PreSerialize(BinaryWriter writer)
    {
        writer.Write((byte)PacketType.ClientState);
        writer.Write(clientId);
    }

    protected override void PreDeserialize(BinaryReader reader)
    {
        reader.ReadByte(); // PacketType.ClientState
        reader.ReadByte(); // clientId
    }

    protected override void PostSerialize(BinaryWriter writer)
        => writer.Write((byte)PacketType.EndOfData);
}