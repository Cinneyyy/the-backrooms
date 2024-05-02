namespace Backrooms.Online.Generic;

public enum PacketType : byte
{
    /// <summary>State of a specific client</summary>
    ClientState = 0b01 << 6,
    /// <summary>State of the server</summary>
    ServerState = 0b10 << 6,
    /// <summary>A request from the server to the client</summary>
    ServerRequest = 0b00 << 6,
    /// <summary>The first packet that the client receives from the server, containing information about the server and client states</summary>
    WelcomePacket = 0b11 << 6,
    /// <summary>Indicates the current stream of data is over, but the packet could still go on</summary>
    EndOfData = 0xff
}