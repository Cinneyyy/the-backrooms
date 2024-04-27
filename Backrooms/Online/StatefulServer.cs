using System;

namespace Backrooms.Online;

public class StatefulServer<TState, TKey> : Server where TState : IState<TKey> where TKey : Enum
{
    public TState state;


    public StatefulServer(bool printDebug) : base(printDebug)
        => handlePacket += (int client, byte[] packet, int packetLen) => {
            if((PacketType)packet[0] == PacketType.ServerState)
                state.Deserialize(packet, 1, packetLen);
        };


    public void BroadcastStateData(params TKey[] dataKeys)
        => BroadcastPacket(state.Serialize(dataKeys));
}
