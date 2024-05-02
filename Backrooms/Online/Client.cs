using System;
using System.Net.Sockets;
using System.Threading;

namespace Backrooms.Online;

public class Client(bool printDebug)
{
    public const int BUFFER_SIZE = Server.BUFFER_SIZE;

    public bool printDebug = printDebug;
    public event Action connect, disconnect;
    public event Action<byte[], int> handlePacket, handleServerRequest, handleServerState;
    public event Action<byte, byte[], int> handleClientState;

    protected TcpClient remoteClient;


    public bool isConnected { get; private set; }


    public void Connect(string ipAddress, int port)
    {
        try
        {
            remoteClient = new(ipAddress, port);
            isConnected = true;
            connect?.Invoke();

            PrintIf(printDebug, $"Connected to server at {ipAddress}:{port}");

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
        PrintIf(printDebug, "Disconnected");
    }

    public void SendPacket(byte[] packet)
    {
        try
        {
            Assert(packet.Length <= BUFFER_SIZE, $"Attempting to send packet of size {packet.Length} [bytes], while the max packet size is {BUFFER_SIZE} [bytes]");

            PrintIf(printDebug, $"Sending packet of size {packet.Length} [bytes] to server");
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
                PrintIf(printDebug, $"Received server packet of size {bytesRead} [bytes]");

                handlePacket?.Invoke(buf, bytesRead);

                byte typeByte = (byte)(buf[0] & (0b11 << 6));
                if(typeByte == (byte)PacketType.ClientState)
                    handleClientState?.Invoke(buf[1], buf, bytesRead);
                ((PacketType)typeByte switch {
                    PacketType.ServerRequest => handleServerRequest,
                    PacketType.ServerState => handleServerState,
                    _ => null
                })?.Invoke(buf, bytesRead);
            }
        }
        catch(Exception exc)
        {
            OutErr(exc);
        }
    }


    private static void PrintIf(bool condition, object msg) => Globals.OutIf(condition, "[Client] " + msg);
}