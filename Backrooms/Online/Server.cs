using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Backrooms.Online;

public abstract class Server<TState>(TState defaultState, bool printDebug = false) where TState : IState
{
    public const int BUFFER_SIZE = 1024;

    public TState state = defaultState;
    public bool printDebug = printDebug;

    protected TcpListener listener;
    protected readonly List<TcpClient> clients = [];


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
        foreach(TcpClient client in clients)
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
        foreach(TcpClient client in clients)
            client.GetStream().Write(packet, 0, packet.Length);
    }

    public void BroadcastStateData(byte[] fieldKeys)
        => BroadcastPacket(state.Serialize(fieldKeys));


    private void AcceptClients()
    {
        while(isHosting)
        {
            TcpClient client = listener.AcceptTcpClient();
            clients.Add(client);

            OutIf(printDebug, $"Client connected: {client.Client.RemoteEndPoint}");

            new Thread(() => HandleClient(client)).Start();
        }
    }

    private void HandleClient(TcpClient client)
    {
        try
        {
            using NetworkStream stream = client.GetStream();
            byte[] buf = new byte[BUFFER_SIZE];
            int bytesRead;

            while((bytesRead = stream.Read(buf, 0, BUFFER_SIZE)) > 0)
            {
                OutIf(printDebug, $"Received client packet of size {bytesRead} [bytes] from client {client.Client.RemoteEndPoint}");
                state.Deserialize(buf, bytesRead);
            }

            OutIf(printDebug, $"Client disconnected: {client.Client.RemoteEndPoint}");
            clients.Remove(client);
            client.Close();
        }
        catch(Exception exc)
        {
            OutErr(exc);
        }
    }
}