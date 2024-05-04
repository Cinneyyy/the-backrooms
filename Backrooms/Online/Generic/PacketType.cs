namespace Backrooms.Online.Generic;

public enum PacketType : byte
{
    /// <summary>State of a specific client</summary>
    ClientState = 0b100 << 5,
    /// <summary>State of the server</summary>
    ServerState = 0b010 << 5,
    /// <summary>The first packet that the client receives from the server, containing information about the server and client states</summary>
    WelcomePacket = 0b110 << 5,
    /// <summary>A request from the server to the client</summary>
    ServerRequest = 0b101 << 5,
    /// <summary>A request from the client to the server</summary>
    ClientRequest = 0b011 << 5,
    /// <summary>Indicates the current stream of data is over, but the packet could still go on</summary>
    EndOfData = 0xff
}