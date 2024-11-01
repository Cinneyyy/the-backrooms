using Backrooms.Serialization;

namespace Backrooms.Online;

public sealed class WelcomePacket<TSState, TCState>() : Packet<WelcomePacket<TSState, TCState>>() where TSState : Packet<TSState>, new() where TCState : Packet<TCState>, new()
{
    public ushort clientId;
    public TSState serverState;
    public ArrElem<TCState>[] clientStateValues;
    public ArrElem<ushort>[] clientStateKeys;
}