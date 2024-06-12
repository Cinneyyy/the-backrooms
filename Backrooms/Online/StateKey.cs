using Backrooms.Online.Generic;

namespace Backrooms.Online;

public enum StateKey : byte
{
    Server = PacketType.ServerState,
    Client = PacketType.ClientState,
    EndOfData = PacketType.EndOfData,

    S_LevelSeed  = Server | 1,

    C_ClientId = Client | 1,
    C_Pos = Client | 2,
    C_Rot = Client | 3,
    C_Skin = Client | 4
}
