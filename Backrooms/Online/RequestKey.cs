using Backrooms.Online.Generic;

namespace Backrooms.Online;

public enum RequestKey
{
    Server = PacketType.ServerRequest,

    S_RegenerateMap = Server | 1
}