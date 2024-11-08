using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Backrooms.Online;

public static class Server
{
    private static TcpListener listener;
    private static readonly Dictionary<byte, (TcpClient client, NetworkStream stream, byte[] buf)> clients = [];
    private static readonly List<byte> clientIds = [];
    private static byte nextClientId;


    public static bool isRunning { get; private set; }
    public static int port { get; private set; }


    public static void StartHost(int port)
    {
        if(isRunning)
        {
            Console.WriteLine("Server is already running");
            return;
        }

        Server.port = port;
        isRunning = true;

        listener = new(IPAddress.Any, port);
        listener.Start();

        Window.pulse += AcceptClients;
        Window.pulse += PollClients;

        Console.WriteLine($"Started hosting on port {port}");
    }

    public static void StopHost()
    {
        if(!isRunning)
        {
            Console.WriteLine("Server is not running");
            return;
        }

        port = 0;
        isRunning = false;
        nextClientId = 0;

        listener.Stop();
        listener.Dispose();
        listener = null;

        Window.pulse -= AcceptClients;
        Window.pulse -= PollClients;

        foreach(TcpClient client in clients.Select(kvp => kvp.Value.client))
            if(client.Connected)
                client.Close();

        clientIds.Clear();
        clients.Clear();
    }


    private static void AcceptClients()
    {
        while(listener.Pending())
        {
            TcpClient client = listener.AcceptTcpClient();
            byte clientId = nextClientId++;

            OnClientConnect(client, clientId);

            Console.WriteLine($"New client connected at {client.Client.RemoteEndPoint}, id #{clientId}");
        }
    }

    private static void OnClientConnect(TcpClient client, byte id)
    {
        clientIds.Add(id);

        client.ReceiveBufferSize = Constants.BufferSize;
        client.SendBufferSize = Constants.BufferSize;

        byte[] buf = new byte[Constants.BufferSize];
        clients[id] = (client, client.GetStream(), buf);

        ConstructWelcomePacket(id);
        ConstructIntegrationPackets(id);
    }

    private static void DisconnectClient(byte id, bool alreadyClosed)
    {
        if(!alreadyClosed)
            clients[id].client.Close();

        clients.Remove(id);
    }

    private static void ConstructWelcomePacket(byte client)
    {
    }

    private static void ConstructIntegrationPackets(byte newClient)
    {
    }

    private static void PollClients()
    {
        foreach(byte id in clientIds)
        {
            (TcpClient client, NetworkStream stream, byte[] buf) = clients[id];

            if(!client.Connected)
            {
                DisconnectClient(id, true);
                continue;
            }

            if(client.Available == 0)
                continue;

            stream.Read(buf, 0, client.Available);

            byte packetType = buf[0];
            Span<byte> data = buf.AsSpan(1, client.Available-1);


        }
    }
}