namespace Backrooms.Online;

public enum DataKey : byte
{
    Server = 1 << 7,
    Client = 0 << 7,

    S_OlafPos   = Server | 1,
    S_LevelSeed = Server | 2,

    C_Pos = Client | 1,
    C_Rot = Client | 2
}