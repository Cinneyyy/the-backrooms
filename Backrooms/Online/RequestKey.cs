using Backrooms.Online.Generic;

namespace Backrooms.Online;

public enum RequestKey
{
    Server = PacketType.ServerRequest,
    Client = PacketType.ClientRequest,

    S_RegenerateMap = Server | 1,

    C_MakeMeOlafTarget = Client | 1
}