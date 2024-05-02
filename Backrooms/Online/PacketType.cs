namespace Backrooms.Online;

public enum PacketType : byte
{
    /// <summary>State of a specific client</summary>
    ClientState = 0b00 << 6,
    /// <summary>State of the server</summary>
    ServerState = 0b01 << 6,
    /// <summary>Request from the client to the server</summary>
    ClientRequest = 0b10 << 6,
    /// <summary>Request form the server to the client</summary>
    ServerRequest = 0b11 << 6
}