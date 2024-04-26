using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Backrooms.Online;

public abstract class Client<TState>(TState defaultState, bool printDebug = false) where TState : IState
{
    public const int BUFFER_SIZE = 1024;

    public TState state = defaultState;
    public bool printDebug = printDebug;

    protected TcpClient remoteClient;


    public bool isConnected { get; private set; }


    public void Connect(string ipAddress, int port)
    {
        try
        {
            remoteClient = new(ipAddress, port);
            isConnected = true;

            OutIf(printDebug, $"Connected to server at {ipAddress}:{port}");

            new Thread(HandleServerCommunication).Start();
        }
        catch(Exception exc)
        {
            OutErr(exc);
        }
    }

    public void Disconnect()
    {
        isConnected = false;
        remoteClient.Close();
        OutIf(printDebug, "Disconnected");
    }

    public void SendPacket(byte[] packet)
    {
        try
        {
            remoteClient.GetStream().Write(packet, 0, packet.Length);
        }
        catch(Exception exc)
        {
            OutErr(exc);
        }
    }

    public void SendStateData(byte[] fieldKeys)
        => SendPacket(state.Serialize(fieldKeys));

    
    private void HandleServerCommunication()
    {
        try
        {
            NetworkStream stream = remoteClient.GetStream();
            byte[] buf = new byte[BUFFER_SIZE];
            int bytesRead;

            while((bytesRead = stream.Read(buf, 0, buf.Length)) > 0)
            {
                OutIf(printDebug, $"Received server packet of size {bytesRead} [bytes]");
                state.Deserialize(buf, bytesRead);
            }
        }
        catch(Exception exc)
        {
            OutErr(exc);
        }
    }
}