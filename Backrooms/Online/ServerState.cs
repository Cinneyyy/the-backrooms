using System;
using System.IO;
using System.Linq;
using Backrooms.Online.Generic;

namespace Backrooms.Online;

/// <summary>
/// Layout of serialized ServerState ([..] == 1 byte, {..} == x times): <br />
/// <code>
/// [PacketType.ServerState as byte]
/// {
///     [StateKey as byte]
///     { [data] } (* size of data in bytes)
/// } (* amount of data keys)
/// [PacketType.EndOfData]
/// </code>
/// </summary>
public class ServerState : State<StateKey>
{
    public int levelSeed;

    public static readonly StateKey[] allKeys = (from v in Enum.GetValues<StateKey>()
                                                 where (v != StateKey.Server) && (((byte)v & 0b1100_0000) == (byte)StateKey.Server)
                                                 select v).ToArray();


    protected override StateKey KeyFromByte(byte value) => (StateKey)value;
    protected override byte ByteFromKey(StateKey key) => (byte)key;

    protected override void SerializeField(BinaryWriter writer, StateKey key)
    {
        switch(key)
        {
            case StateKey.S_LevelSeed: writer.Write(levelSeed); break;
            default: throw new($"Invalid StateKey in ServerState.SerializeField(): {key} // {(byte)key}");
        }
    }

    protected override void DeserializeField(BinaryReader reader, StateKey key)
    {
#pragma warning disable IDE0066
        switch(key)
        {
            case StateKey.S_LevelSeed: levelSeed = reader.ReadInt32(); break;
            default: throw new($"Invalid StateKey in ServerState.DeserializeField(): {key} // {(byte)key}");
        }
    }

    protected override void PreSerialize(BinaryWriter writer)
        => writer.Write((byte)PacketType.ServerState);

    protected override void PreDeserialize(BinaryReader reader)
        => reader.ReadByte();

    protected override void PostSerialize(BinaryWriter writer)
        => writer.Write((byte)PacketType.EndOfData);
}