namespace Backrooms.Online;

public enum PacketType : byte
{
    /// <summary>Initial packet every client receives, containing info about its id, server state and client states</summary>
    WelcomePacket,
    /// <summary>Packet containing info about the current state of the server (seed, entity positions, etc.)</summary>
    ServerState,
    /// <summary>Packet containing info about a specific client (position, rotation, etc.)</summary>
    ClientState,
    /// <summary>Packet containing info about a request to the server</summary>
    ServerReq,
    /// <summary>Packet containing info about a request to a client/multiple clients</summary>
    ClientReq,
    IntegrateClient,
    Misc
}