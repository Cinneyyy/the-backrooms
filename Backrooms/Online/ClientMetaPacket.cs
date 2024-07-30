namespace Backrooms.Online;

public class ClientMetaPacket(ushort id) : Packet<ClientMetaPacket>()
{
    public ushort clientId = id;


    public ClientMetaPacket() : this(0) { }
}