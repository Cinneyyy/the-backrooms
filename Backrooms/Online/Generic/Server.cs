using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;

namespace Backrooms.Online.Generic;

#pragma warning disable CS0067
public class Server(int bufSize = 256, bool printDebug = false)
{
    public readonly int bufSize = bufSize;
    public bool printDebug = printDebug;
    public event Action<byte> connect, disconnect;
    public event Action<byte, byte[], int> handlePacket, handleClientRequest, handleClientState;
    public event Action<byte[], int> handleServerState;
    public event Func<byte, byte[]> constructWelcomePacket;

    protected TcpListener listener;
    protected readonly List<(TcpClient client, byte id)> clients = [];

    private byte nextClientId = 1; // One-indexed


    public bool isHosting { get; private set; }


    public void StartHosting(int port)
    {
        try
        {
            listener = new(IPAddress.Any, port);
            listener.Start();
            isHosting = true;

            Out($"Hosting on port {port}");

            new Thread(AcceptClients).Start();
        }
        catch(Exception exc)
        {
            OutErr(exc);
        }
    }

    public void StopHosting()
    {
        foreach(TcpClient client in from c in clients select c.client)
            client.Close();
        clients.Clear();

        isHosting = false;
        listener.Stop();

        Out("Stopped hosting");
    }

    public void BroadcastPacket(byte[] packet, int length = -1)
    {
        int len = length == -1 ? packet.Length : length;

        Assert(len <= bufSize, $"Attempting to send packet of size {len} [bytes], while the buffer size is merely {bufSize} bytes");

        PrintIf(printDebug, $"Sending data packet of size {len} [bytes] to all ({clients.Count}) clients");
        foreach(TcpClient client in from c in clients select c.client)
            client.GetStream().Write(packet, 0, len);
    }
    public void BroadcastPacket(byte[] packet, byte[] excluded, int length = -1)
    {
        int len = length == -1 ? packet.Length : length;

        Assert(len <= bufSize, $"Attempting to send packet of size {len} [bytes], while the buffer size is merely {bufSize} bytes");

        PrintIf(printDebug, $"Sending data packet of size {len} [bytes] to all ({clients.Count}) clients");
        foreach(TcpClient client in from c in clients 
                                    where !excluded.Contains(c.id) 
                                    select c.client)
            client.GetStream().Write(packet, 0, len);
    }

    public void SendPacket(byte clientId, byte[] packet, int length = -1)
    {
        int len = length == -1 ? packet.Length : length;

        Assert(len <= bufSize, $"Attempting to send packet of size {len} [bytes], while the buffer size is merely {bufSize} bytes");

        PrintIf(printDebug, $"Sending data packet of size {len} [bytes] to client with id {clientId}");
        GetClient(clientId).GetStream().Write(packet, 0, len);
    }


    protected TcpClient GetClient(int clientId)
        => clients.Find(c => c.id == clientId).client;


    private void AcceptClients()
    {
        while(isHosting)
        {
            TcpClient client = listener.AcceptTcpClient();
            byte clientId = nextClientId++;
            clients.Add((client, clientId));
            connect?.Invoke(clientId);

            PrintIf(printDebug, $"Client connected: {client.Client.RemoteEndPoint}");

            new Thread(() => HandleClient(client, clientId)).Start();
        }
    }

    private void HandleClient(TcpClient client, byte clientId)
    {
        try
        {
            using NetworkStream stream = client.GetStream();
            byte[] buf = new byte[bufSize];
            int bytesRead;

            if(constructWelcomePacket is not null)
                stream.Write(constructWelcomePacket(clientId));

            while((bytesRead = stream.Read(buf, 0, bufSize)) > 0)
            {
                PrintIf(printDebug, $"Received client packet of size {bytesRead} [bytes] from client {client.Client.RemoteEndPoint}");

                handlePacket?.Invoke(clientId, buf, bytesRead);

                PacketType packetType = (PacketType)(byte)(buf[0] & 0b11 << 6);
                if(packetType == PacketType.ServerState)
                    handleServerState?.Invoke(buf, bytesRead);
                else if(packetType == PacketType.ClientState)
                    handleClientState?.Invoke(clientId, buf, bytesRead);
            }

            PrintIf(printDebug, $"Client disconnected: {client.Client.RemoteEndPoint}");
            clients.RemoveAll(c => c.id == clientId);
            client.Close();
        }
        catch(Exception exc)
        {
            OutErr(exc);
        }
    }


    private static void PrintIf(bool condition, object msg) => OutIf(condition, "[Server] " + msg);
}