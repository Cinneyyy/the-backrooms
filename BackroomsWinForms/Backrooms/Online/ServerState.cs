using Backrooms.Entities;
using Backrooms.Serialization;

namespace Backrooms.Online;

public class ServerState() : Packet<ServerState>()
{
    public int levelSeed;
}