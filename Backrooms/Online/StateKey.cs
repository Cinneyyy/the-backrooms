using Backrooms.Online.Generic;

namespace Backrooms.Online;

public enum StateKey : byte
{
    Server = PacketType.ServerState,
    Client = PacketType.ClientState,
    EndOfData = PacketType.EndOfData,

    S_LevelSeed  = Server | 1,
    S_OlafPos    = Server | 2,
    S_OlafTarget = Server | 3,

    C_ClientId = Client | 1,
    C_Pos = Client | 2,
    C_Rot = Client | 3
}
