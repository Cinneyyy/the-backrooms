using System;
using System.Collections.Generic;
using Backrooms.OnlineNew.Generic;

namespace Backrooms.OnlineNew;

public class MpManager<TSState, TCState> where TSState : Packet<TSState>, new() where TCState : Packet<TCState>, new()
{
    public event Action onFinishConnect;
    public event Action<ushort> onPlayerConnect;
    public readonly CommonState commonState = new();
    public readonly Dictionary<ushort, TCState> clientStates = [];


    public TSState serverState { get; private set; }
    public bool isConnected { get; private set; }
    public Server<TSState, TCState> server { get; private set; }
    public Client<TSState, TCState> client { get; private set; }
    public TCState ownClientState { get; private set; }
    public ushort ownClientId { get; private set; }
    public string ipAddress { get; private set; }
    public int port { get; private set; }


    public void Start(bool host, string ipAddress, int port)
    {
        isConnected = true;
        this.ipAddress = ipAddress;
        this.port = port;

        if(host)
        {
            server = new(this);
            server.StartHost(port);
        }

        client = new(this);
        client.Connect(ipAddress, port);
    }

    public void HandleWelcomePacket(WelcomePacket<TSState, TCState> welcomePacket)
    {
        ownClientId = welcomePacket.clientId;
        serverState = welcomePacket.serverState;
        clientStates.Clear();

        for(int i = 0; i < welcomePacket.clientStateKeys.length; i++)
            clientStates[welcomePacket.clientStateKeys[i]] = welcomePacket.clientStateValues[i];

        ownClientState = clientStates[ownClientId];
    }
}