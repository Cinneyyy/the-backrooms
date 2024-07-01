using Backrooms.Serialization;

namespace Backrooms.OnlineNew.Generic;

public class WelcomePacket<TSState, TCState> : Packet<WelcomePacket<TSState, TCState>> where TSState : Packet<TSState> where TCState : Packet<TCState>
{
    public ushort clientId;
    public TSState serverState;
    public Arr<TCState> clientStates;
}