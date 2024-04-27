using System;

namespace Backrooms.Online;

public class StatefulClient<TState, TKey> : Client where TState : IState<TKey> where TKey : Enum
{
    public TState state;


    public StatefulClient(bool printDebug) : base(printDebug)
        => handlePacket += (byte[] packet, int packetLen) => {
            if((PacketType)packet[0] == PacketType.ClientState)
                state.Deserialize(packet, 1, packetLen);
        };

    public void SendStateData(params TKey[] dataKeys)
        => SendPacket(state.Serialize(dataKeys));
}