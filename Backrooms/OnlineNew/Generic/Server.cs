﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Backrooms.Serialization;

namespace Backrooms.OnlineNew.Generic;

public class Server<TSState, TCState>(MpManager<TSState, TCState> mpManager) where TSState : Packet<TSState>, new() where TCState : Packet<TCState>, new()
{
    public delegate void ReceivePacketHandler(PacketType type, byte[] data, ushort clientId);


    public readonly CommonState commonState = mpManager.commonState;
    public readonly MpManager<TSState, TCState> mpManager = mpManager;
    public readonly List<ushort> clientIds = [];
    public event ReceivePacketHandler receivePacket;

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

            listener = new(IPAddress.Any, port);
            openPort = port;
            isHosting = true;

            Out($"Hosting on port {port}");

            new Thread(WelcomeClients).Start();
        }
        catch(Exception exc)
        {
            Out($"{exc.GetType()} in Server.StartHost ;; {exc.Message}", ConsoleColor.Red);
        }
    }

    public void StopHost()
    {
        if(!isHosting)
        {
            Out($"Cannot stop host while server is not hosting");
            return;
        }

        isHosting = false;
        listener.Stop();

        Out($"Stopped hosting on port {openPort}");

        openPort = 0;
    }

    public void SendPacket<T>(PacketType type, T packet, string[] members, ushort[] clientIds) where T : Packet<T>, new()
    {
        try
        {
            byte[] data = members is null or []
                ? BinarySerializer<T>.Serialize(packet, commonState.packetCompression)
                : BinarySerializer<T>.SerializeMembers(packet, members, commonState.packetCompression);

            if(data.Length > commonState.bufSize)
                throw new($"Attempted to send packet with a larger size than the buffer size allows {data.Length} / {commonState.bufSize}");

            data = [(byte)type, ..data];
            foreach(ushort clientId in clientIds)
                remoteClients[clientId].GetStream().Write(data, 0, data.Length);
        }
        catch(Exception exc)
        {
            Out($"{exc.GetType()} in Client.SendPacket ;; {exc.Message}", ConsoleColor.Red);
        }
    }

    public void BroadcastPacket<T>(PacketType type, T packet, string[] members, ushort[] excludedClientIds) where T : Packet<T>, new()
        => SendPacket(type, packet, members, (from id in clientIds where !excludedClientIds.Contains(id) select id).ToArray());
    
    public void SendPacketRaw(byte[] data, ushort[] clientIds)
    {
        try
        {
            foreach(ushort clientId in clientIds)
                remoteClients[clientId].GetStream().Write(data, 0, data.Length);
        }
        catch(Exception exc)
        {
            Out($"{exc.GetType()} in Client.SendPacketRaw ;; {exc.Message}", ConsoleColor.Red);
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
            while(!clientIds.Contains(clientId));

            remoteClients[clientId] = client;
            clientIds.Add(clientId);

            Out($"New client connected: {client.Client.RemoteEndPoint}");

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
            clientStateValues = [..mpManager.clientStates.Values],
            clientStateKeys = [..mpManager.clientStates.Keys]
        };

        SendPacket(PacketType.WelcomePacket, welcomePacket, null, [clientId]);

        while(isHosting && client.Connected && stream.Read(buf, 0, buf.Length) is int bytesRead && bytesRead > 0)
        {
            try
            {
                OutIf(commonState.printDebug, $"Received client packet from client #{clientId} with size {bytesRead} bytes");

                PacketType type = (PacketType)buf[0];
                byte[] data = buf[1..bytesRead];

                switch(type)
                {
                    case PacketType.ClientState or PacketType.ServerState:
                        BroadcastPacketRaw(buf[..bytesRead], [clientId]);
                        break;
                    case PacketType.WelcomePacket:
                        throw new($"Server should not receive packet of type 'WelcomPacket', as it is only meant for clients");
                    default: 
                        receivePacket?.Invoke(type, buf[1..bytesRead], clientId); 
                        break;
                }
            }
            catch(Exception exc)
            {
                Out($"{exc.GetType()} in Server.ManageClient (client {clientId})", ConsoleColor.Red);
            }
        }

        Out($"Client disconnected: {client.Client.RemoteEndPoint}");
        remoteClients.Remove(clientId);
        client.Close();
    }
}