namespace Backrooms.Online;

public enum PacketType : byte
{
    /// <summary>State of a specific client</summary>
    ClientState,
    /// <summary>State of the server</summary>
    ServerState,
    /// <summary>Request from the client to the server</summary>
    ClientRequest,
    /// <summary>Request form the server to the client</summary>
    ServerRequest
}