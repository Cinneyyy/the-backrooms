using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;

namespace Backrooms.Online;

public class Server(bool printDebug = false)
{
    public const int BUFFER_SIZE = 1024;

    public bool printDebug = printDebug;
    public event Action<int> connect, disconnect;
    public event Action<int, byte[], int> handlePacket, handleClientRequest;

    protected TcpListener listener;
    protected readonly List<(TcpClient client, int id)> clients = [];

    private int nextClientId;


    public bool isHosting { get; private set; }


    public void StartHosting(int port)
    {
        try
        {
            listener = new(IPAddress.Any, port);
            listener.Start();
            isHosting = true;

            OutIf(printDebug, $"Hosting on port {port}");

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

        OutIf(printDebug, "Stopped hosting");
    }

    public void BroadcastPacket(byte[] packet)
    {
        Assert(packet.Length <= BUFFER_SIZE, $"Attempting to send packet of size {packet.Length} [bytes], while the buffer size is merely {BUFFER_SIZE} bytes");

        OutIf(printDebug, $"Sending data packet of size {packet.Length} [bytes] to all ({clients.Count}) clients");
        foreach(TcpClient client in from c in clients select c.client)
            client.GetStream().Write(packet, 0, packet.Length);
    }

    public void SendPacket(int clientId, byte[] packet)
    {
        Assert(packet.Length <= BUFFER_SIZE, $"Attempting to send packet of size {packet.Length} [bytes], while the buffer size is merely {BUFFER_SIZE} bytes");

        OutIf(printDebug, $"Sending data packet of size {packet.Length} [bytes] to client with id {clientId}");
        GetClient(clientId).GetStream().Write(packet, 0, packet.Length);
    }


    protected TcpClient GetClient(int clientId)
        => clients.Find(c => c.id == clientId).client;


    private void AcceptClients()
    {
        while(isHosting)
        {
            TcpClient client = listener.AcceptTcpClient();
            int clientId = nextClientId++;
            clients.Add((client, clientId));
            connect?.Invoke(clientId);

            OutIf(printDebug, $"Client connected: {client.Client.RemoteEndPoint}");

            new Thread(() => HandleClient(client, clientId)).Start();
        }
    }

    private void HandleClient(TcpClient client, int clientId)
    {
        try
        {
            using NetworkStream stream = client.GetStream();
            byte[] buf = new byte[BUFFER_SIZE];
            int bytesRead;

            while((bytesRead = stream.Read(buf, 0, BUFFER_SIZE)) > 0)
            {
                OutIf(printDebug, $"Received client packet of size {bytesRead} [bytes] from client {client.Client.RemoteEndPoint}");

                handlePacket?.Invoke(clientId, buf, bytesRead);

                if((PacketType)buf[0] == PacketType.ClientRequest)
                    handleClientRequest?.Invoke(clientId, buf, bytesRead);
            }

            OutIf(printDebug, $"Client disconnected: {client.Client.RemoteEndPoint}");
            clients.RemoveAll(c => c.id == clientId);
            client.Close();
        }
        catch(Exception exc)
        {
            OutErr(exc);
        }
    }
}