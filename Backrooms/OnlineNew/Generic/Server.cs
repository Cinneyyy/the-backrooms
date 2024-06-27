using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Backrooms.Serialization;

namespace Backrooms.OnlineNew.Generic;

public class Server<TSState, TCState>(CommonState commonState) where TSState : Packet<TSState> where TCState : Packet<TCState>
{
    public delegate void ReceivePacketHandler(PacketType type, byte[] data, ushort clientId);


    public readonly CommonState commonState = commonState;
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
                : BinarySerializer<T>.Serialize(packet, members, commonState.packetCompression);

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


    private void WelcomeClients()
    {
        while(isHosting)
        {
            TcpClient client = listener.AcceptTcpClient();
            ushort clientId = (ushort)RNG.Range(ushort.MaxValue);
            remoteClients[clientId] = client;

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
            //serverState = 
        };

        SendPacket(PacketType.WelcomePacket, welcomePacket, null, [clientId]);
        // TODO: set all fields, receive welcome packet

        while(isHosting && client.Connected && stream.Read(buf, 0, buf.Length) is int bytesRead && bytesRead > 0)
        {
            try
            {
                OutIf(commonState.printDebug, $"Received client packet from client #{clientId} with size {bytesRead} bytes");

                PacketType type = (PacketType)buf[0];
                receivePacket?.Invoke(type, buf[1..bytesRead], clientId);
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