namespace Backrooms.Online;

public class ClientState() : Packet<ClientState>()
{
    public Vec2f pos;
    public float rot;
}