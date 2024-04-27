using System;
using System.Net.Sockets;
using System.Threading;

namespace Backrooms.Online;

public class Client(bool printDebug)
{
    public const int BUFFER_SIZE = Server.BUFFER_SIZE;

    public bool printDebug = printDebug;
    public event Action connect, disconnect;
    public event Action<byte[], int> handlePacket, handleClientRequest;

    protected TcpClient remoteClient;


    public bool isConnected { get; private set; }


    public void Connect(string ipAddress, int port)
    {
        try
        {
            remoteClient = new(ipAddress, port);
            isConnected = true;
            connect?.Invoke();

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
        disconnect?.Invoke();
        OutIf(printDebug, "Disconnected");
    }

    public void SendPacket(byte[] packet)
    {
        try
        {
            Assert(packet.Length <= BUFFER_SIZE, $"Attempting to send packet of size {packet.Length} [bytes], while the max packet size is {BUFFER_SIZE} [bytes]");

            OutIf(printDebug, $"Sending packet of size {packet.Length} [bytes] to server");
            remoteClient.GetStream().Write(packet, 0, packet.Length);
        }
        catch(Exception exc)
        {
            OutErr(exc);
        }
    }

    
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

                handlePacket?.Invoke(buf, bytesRead);

                if((PacketType)buf[0] == PacketType.ServerRequest)
                    handleClientRequest?.Invoke(buf, bytesRead);
            }
        }
        catch(Exception exc)
        {
            OutErr(exc);
        }
    }
}