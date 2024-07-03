namespace Backrooms.OnlineNew;

public class IntegrationPacket<TCState>() : Packet<IntegrationPacket<TCState>>() where TCState : Packet<TCState>, new()
{
    public ushort id;
    public TCState state;
}