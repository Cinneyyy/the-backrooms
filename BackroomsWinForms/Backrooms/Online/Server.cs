using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Backrooms.Serialization;

namespace Backrooms.Online;

public class Server<TSState, TCState, TReq>(MpManager<TSState, TCState, TReq> mpManager)
    where TSState : Packet<TSState>, new()
    where TCState : Packet<TCState>, new()
    where TReq : struct, Enum
{
    public delegate void ReceivePacketHandler(PacketType type, byte[] data, ushort clientId);


    public readonly CommonState commonState = mpManager.commonState;
    public readonly MpManager<TSState, TCState, TReq> mpManager = mpManager;
    public readonly List<ushort> clientIds = [];
    public event ReceivePacketHandler receivePacket;
    public event MpManager<TSState, TCState, TReq>.RequestHandler receiveRequest;

    private TcpListener listener;
    private readonly Dictionary<ushort, TcpClient> remoteClients = [];


    public bool isHosting { get; private set; }
    public int openPort { get; private set; }


    public void StartHost(int port)
    {
        try
        {
            if(isHosting)
                throw new($"Server is already hosting");

            openPort = port;
            isHosting = true;

            listener = new(IPAddress.Any, port);
            listener.Start();

            Out(Log.Server, $"Hosting on port {port}");

            new Thread(WelcomeClients).Start();
        }
        catch(Exception exc)
        {
            OutErr(Log.Server, exc, $"{exc.GetType()} in Server.StartHost ;; $e");
        }
    }

    public void StopHost()
    {
        if(!isHosting)
        {
            Out(Log.Server, $"Cannot stop host while server is not hosting");
            return;
        }

        isHosting = false;
        listener.Stop();

        Out(Log.Server, $"Stopped hosting on port {openPort}");

        openPort = 0;
    }

    public void SendPacket<T>(PacketType type, T packet, string[] members, ushort[] clientIds) where T : Packet<T>, new()
    {
        try
        {
            if(clientIds is [])
                return;

            byte[] data = members is null or []
                ? BinarySerializer<T>.Serialize(packet, commonState.packetCompression)
                : BinarySerializer<T>.SerializeMembers(packet, members, commonState.packetCompression);

            if(data.Length > commonState.bufSize)
                throw new($"Attempted to send packet with a larger size than the buffer size allows {data.Length} / {commonState.bufSize}");

            data = [(byte)type, ..data];
            foreach(ushort clientId in clientIds)
                remoteClients[clientId].GetStream().Write(data, 0, data.Length);

            OutIf(Log.Server, commonState.logPackets, $"Sent {data.Length} byte long packet to {clientIds} clients, packet type: {type} // {typeof(T)}");
        }
        catch(Exception exc)
        {
            OutErr(Log.Server, exc, $"{exc.GetType()} in Client.SendPacket ;; $e");
        }
    }

    public void BroadcastPacket<T>(PacketType type, T packet, string[] members, ushort[] excludedClientIds) where T : Packet<T>, new()
        => SendPacket(type, packet, members, (from id in clientIds where !excludedClientIds.Contains(id) select id).ToArray());

    public void SendPacketRaw(byte[] data, ushort[] clientIds)
    {
        try
        {
            if(clientIds is [])
                return;

            foreach(ushort clientId in clientIds)
                remoteClients[clientId].GetStream().Write(data, 0, data.Length);

            OutIf(Log.Server, commonState.logPackets, $"Sent {data.Length} byte long raw packet to {clientIds.Length} clients");
        }
        catch(Exception exc)
        {
            OutErr(Log.Server, exc, $"{exc.GetType()} in Server.SendPacketRaw ;; $e");
        }
    }

    public void BroadcastPacketRaw(byte[] data, ushort[] excludedClientIds)
        => SendPacketRaw(data, (from id in clientIds where !excludedClientIds.Contains(id) select id).ToArray());


    private void WelcomeClients()
    {
        while(isHosting)
        {
            TcpClient client = listener.AcceptTcpClient();

            ushort clientId;
            do clientId = (ushort)RNG.Range(ushort.MaxValue);
            while(clientIds.Contains(clientId));

            remoteClients[clientId] = client;
            clientIds.Add(clientId);
            mpManager.clientStates[clientId] = new();

            Out(Log.Server, $"New client connected: {client.Client.RemoteEndPoint}, id #{clientId}");

            new Thread(() => ManageClient(client, clientId)).Start();
        }
    }

    private void ManageClient(TcpClient client, ushort clientId)
    {
        NetworkStream stream = client.GetStream();
        byte[] buf = new byte[commonState.bufSize];

        WelcomePacket<TSState, TCState> welcomePacket = new() {
            clientId = clientId,
            serverState = mpManager.serverState,
            clientStateValues = (from v in mpManager.clientStates.Values select (ArrElem<TCState>)v).ToArray(),
            clientStateKeys = (from k in mpManager.clientStates.Keys select (ArrElem<ushort>)k).ToArray(),
        };
        IntegrationPacket<TCState> integrationPacket = new() {
            id = clientId,
            state = mpManager.clientStates[clientId]
        };

        Out(Log.Server, $"Sending welcome packet to client #{clientId} and integration packet to all other clients");
        SendPacket(PacketType.WelcomePacket, welcomePacket, null, [clientId]);
        BroadcastPacket(PacketType.IntegrateClient, integrationPacket, null, [clientId]);

        try
        {
            while(isHosting && client.Connected && stream.Read(buf, 0, buf.Length) is int bytesRead && bytesRead > 0)
                try
                {
                    PacketType type = (PacketType)buf[0];
                    byte[] data = buf[1..bytesRead];

                    OutIf(Log.Server, commonState.logPackets, $"Received client packet from client #{clientId} with size {bytesRead} bytes and of type {type}");

                    switch(type)
                    {
                        case PacketType.ClientState or PacketType.ServerState or PacketType.ClientReq:
                            BroadcastPacketRaw(buf[..bytesRead], [clientId]);
                            break;
                        case PacketType.ServerReq:
                        {
                            TReq req = PrimitiveSerializer.Deserialize<TReq>(data);
                            receiveRequest?.Invoke(req);
                            break;
                        }
                        case PacketType.Misc:
                            receivePacket?.Invoke(type, data, clientId);
                            break;
                        default:
                            throw new($"Invalid packet type for server to receive: {type}");
                    }
                }
                catch(Exception exc)
                {
                    OutErr(Log.Server, exc, $"{exc.GetType()} in Server.ManageClient (client #{clientId}) ;; $e");
                }
        }
        catch(Exception exc)
        {
            OutErr(Log.Server, exc, $"{exc.GetType()} in Server.ManageClient, outside while stream (client #{clientId}) ;; $e");
        }
        finally
        {
            Out(Log.Server, $"Client disconnected: {client.Client.RemoteEndPoint}");
            remoteClients.Remove(clientId);
            clientIds.Remove(clientId);
            client.Close();
            BroadcastPacket(PacketType.RemoveClient, new ClientMetaPacket(clientId), null, []);
        }
    }
}