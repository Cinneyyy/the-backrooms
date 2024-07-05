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
    /// <summary>Packet sent when a new client joins and needs to be integrated into all other connected states</summary>
    IntegrateClient,
    /// <summary>Packet sent when a client disconnects and needs all its data removed on all other connected states</summary>
    RemoveClient,
    /// <summary>Custom packets that do not have hardcoded functionality</summary>
    Misc
}