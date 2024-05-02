using System;
using System.Net.Sockets;
using System.Threading;

namespace Backrooms.Online.Generic;

#pragma warning disable CS0067
public class Client(int bufSize = 256, bool printDebug = false)
{
    public int bufSize = bufSize;
    public bool printDebug = printDebug;
    public event Action connect, disconnect;
    public event Action<byte[], int> handlePacket, handleServerRequest, handleServerState, handleWelcomePacket;
    public event Action<byte, byte[], int> handleClientState;

    protected TcpClient remoteClient;


    public bool isConnected { get; private set; }
    public byte ownClientId { get; private set; }


    public void Connect(string ipAddress, int port)
    {
        try
        {
            remoteClient = new(ipAddress, port);
            isConnected = true;
            connect?.Invoke();

            Out($"Connected to server at {ipAddress}:{port}");

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
        Out("Disconnected");
    }

    public void SendPacket(byte[] packet)
    {
        try
        {
            Assert(packet.Length <= bufSize, $"Attempting to send packet of size {packet.Length} [bytes], while the max packet size is {bufSize} [bytes]");

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
            byte[] buf = new byte[bufSize];
            int bytesRead;

            while((bytesRead = stream.Read(buf, 0, buf.Length)) > 0)
            {
                PrintIf(printDebug, $"Received server packet of size {bytesRead} [bytes]");

                handlePacket?.Invoke(buf, bytesRead);

                PacketType packetType = (PacketType)(buf[0] & 0b11 << 6);
                if(packetType == PacketType.ClientState)
                    handleClientState?.Invoke(buf[1], buf, bytesRead); // TODO: make better
                else if(packetType == PacketType.ServerState)
                    handleServerState?.Invoke(buf, bytesRead);
                else if(packetType == PacketType.WelcomePacket)
                    handleWelcomePacket?.Invoke(buf, bytesRead);
                else if(packetType == PacketType.ServerRequest)
                    handleServerRequest?.Invoke(buf, bytesRead);
            }
        }

        catch(Exception exc)
        {
            OutErr(exc);
        }
    }


    private static void PrintIf(bool condition, object msg) => OutIf(condition, "[Client] " + msg);
}