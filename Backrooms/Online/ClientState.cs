using System;
using Backrooms.Serialization;

namespace Backrooms.Online;

public class ClientState() : Packet<ClientState>()
{
    public Vec2f pos;
    public float rot;
    
    [DontSerialize] public SpriteRenderer renderer;
    [DontSerialize] public Action<float> updaterDelegate;
}