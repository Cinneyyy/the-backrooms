using Backrooms.Online.Generic;

namespace Backrooms.Online;

public enum RequestKey
{
    Server = PacketType.ServerRequest,
    Client = PacketType.ClientRequest,

    S_RegenerateMap = Server | 1,
    S_UpdateSkin = Server | 2,

    C_MakeMeOlafTarget = Client | 1,
    C_UpdateSkin = Client | 2
}