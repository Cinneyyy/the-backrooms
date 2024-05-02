namespace Backrooms.Online.Generic;

public enum PacketType : byte
{
    /// <summary>State of a specific client</summary>
    ClientState = 0b01 << 6,
    /// <summary>State of the server</summary>
    ServerState = 0b10 << 6,
    /// <summary>The first packet that the client receives from the server, containing information about the server and client states</summary>
    WelcomePacket = 0b11 << 6,
    /// <summary>Not a packet type per se, but this indicates that the deserialization process should break</summary>
    EndOfData = 0xff
}