using Backrooms.Serialization;

namespace Backrooms.OnlineNew.Generic;

public sealed class WelcomePacket<TSState, TCState>() : Packet<WelcomePacket<TSState, TCState>>() where TSState : Packet<TSState>, new() where TCState : Packet<TCState>, new()
{
    public ushort clientId;
    public TSState serverState;
    public Arr<TCState> clientStateValues;
    public Arr<ushort> clientStateKeys;
}