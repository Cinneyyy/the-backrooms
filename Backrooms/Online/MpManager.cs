using System;
using System.Collections.Generic;
using Backrooms.Serialization;

namespace Backrooms.Online;

public class MpManager<TSState, TCState, TReq> (CommonState commonState = null)
    where TSState : Packet<TSState>, new()
    where TCState : Packet<TCState>, new()
    where TReq : struct, Enum
{
    public delegate void RequestHandler(TReq req);
    public delegate void ClientEvent(ushort id);


    public event Action connectedToServer;
    public event ClientEvent clientConnected, clientDisconnected;
    public event RequestHandler receiveClientRequest, receiveServerRequest;
    public readonly CommonState commonState = commonState ?? new();
    public readonly Dictionary<ushort, TCState> clientStates = [];


    public TSState serverState { get; private set; } = new();
    public bool isConnected { get; private set; }
    public bool isHost { get; private set; }
    public Server<TSState, TCState, TReq> server { get; private set; }
    public Client<TSState, TCState, TReq> client { get; private set; }
    public TCState clientState { get; private set; } = new();
    public ushort clientId { get; private set; }
    public string ipAddress { get; private set; }
    public int port { get; private set; }


    public void Start(bool host, string ipAddress, int port)
    {
        isHost = host;
        this.ipAddress = ipAddress;
        this.port = port;

        if(host)
        {
            server = new(this);
            server.receiveRequest += receiveServerRequest;
            server.StartHost(port);
        }

        client = new(this);
        client.receiveRequest += receiveClientRequest;
        client.clientConnected += clientConnected;
        client.clientDisconnected += clientDisconnected;
        client.Connect(ipAddress, port);
    }

    public void HandleWelcomePacket(WelcomePacket<TSState, TCState> welcomePacket)
    {
        clientId = welcomePacket.clientId;
        serverState = welcomePacket.serverState;
        clientStates.Clear();

        for(int i = 0; i < welcomePacket.clientStateKeys.Length; i++)
            clientStates[welcomePacket.clientStateKeys[i]] = welcomePacket.clientStateValues[i];

        clientState = clientStates[clientId];

        Out(Log.MpManager, $"Handled welcome packet and was assigned id #{clientId}");

        isConnected = true;
        connectedToServer?.Invoke();
    }

    public void SyncClientState(params string[] members)
    {
        if(!isConnected)
            throw new("Cannot sync client state while not connected to server");

        client.SendStateChanges(false, members);
    }

    public void SyncServerState(params string[] members)
    {
        if(!isConnected)
            throw new("Cannot sync server state while not connected to server");

        client.SendStateChanges(true, members);
    }

    public void SendServerReq(TReq req)
        => client.SendPacketRaw([(byte)PacketType.ServerReq, ..PrimitiveSerializer.Serialize(req)]);

    public void SendClientReq(TReq req)
        => client.SendPacketRaw([(byte)PacketType.ClientReq, ..PrimitiveSerializer.Serialize(req)]);
}