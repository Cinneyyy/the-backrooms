namespace Backrooms.Online;

public enum RequestKey : byte
{
    Server = PacketType.ServerRequest,
    Client = PacketType.ClientRequest,

    S_SetClientList  = Server | 1,
    S_SetOwnId       = Server | 2,
    S_PlayerJoined   = Server | 3,
    S_SetServerState = Server | 4
}