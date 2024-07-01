using System;
using System.Net.Sockets;
using System.Threading;
using Backrooms.Serialization;

namespace Backrooms.OnlineNew.Generic;

public class Client<TSState, TCState>(MpManager<TSState, TCState> mpManager) where TSState : Packet<TSState>, new() where TCState : Packet<TCState>, new()
{
    public delegate void ReceivePacketHandler(PacketType type, byte[] data);


    public readonly CommonState commonState = mpManager.commonState;
    public readonly MpManager<TSState, TCState> mpManager = mpManager;
    public event ReceivePacketHandler receivePacket;

    private TcpClient remoteHost;
    private bool receiveInput;


    public bool isConnected { get; private set; }
    public ushort clientId { get; private set; }
    public string remoteIpAddress { get; private set; }
    public int remotePort { get; private set; }


    public void Connect(string ipAddress, int port)
    {
        try
        {
            if(isConnected)
                throw new($"Client is already connected");

            remoteHost = new(ipAddress, port);
            remoteIpAddress = ipAddress;
            remotePort = port;
            receiveInput = true;

            new Thread(HandleServerCommunication).Start();

            Out($"Connected to remote server at {ipAddress}:{port}");
        }
        catch(Exception exc)
        {
            Out($"[{exc.GetType()}] Failed to connect to server ({ipAddress}:{port}) ;; {exc.Message}", ConsoleColor.Red);
        }
    }

    public void Disconnect()
    {
        if(!isConnected)
        {
            Out($"Cannot disconnect while client is not connected to a remote server");
            return;
        }

        isConnected = false;
        receiveInput = false;
        remoteHost.Close();

        Out($"Disconnected from server at {remoteIpAddress}:{remotePort}");

        remoteIpAddress = null;
        remotePort = 0;
    }

    /// <summary>If members is null or [], it will serialize all members</summary>
    public void SendPacket<T>(PacketType type, T packet, string[] members, byte[] prepend = null) where T : Packet<T>, new()
    {
        try
        {
            byte[] data = members is null or []
                ? BinarySerializer<T>.Serialize(packet, commonState.packetCompression)
                : BinarySerializer<T>.SerializeMembers(packet, members, commonState.packetCompression);

            if(data.Length > commonState.bufSize)
                throw new($"Attempted to send packet with a larger size than the buffer size allows {data.Length} / {commonState.bufSize}");

            data = [(byte)type, ..(prepend ?? []), ..data];
            remoteHost.GetStream().Write(data, 0, data.Length);
        }
        catch(Exception exc)
        {
            Out($"{exc.GetType()} in Client.SendPacket ;; {exc.Message}", ConsoleColor.Red);
        }
    }

    public void SendClientStateChanges(string[] members)
        => SendPacket(PacketType.ClientState, mpManager.ownClientState, members);


    private void HandleServerCommunication()
    {
        NetworkStream stream = remoteHost.GetStream();
        byte[] buf = new byte[commonState.bufSize];

        while(receiveInput && stream.Read(buf, 0, buf.Length) is int bytesRead && bytesRead > 0)
        {
            try
            {
                OutIf(commonState.printDebug, $"Received server packet with size {bytesRead} bytes");

                PacketType type = (PacketType)buf[0];
                byte[] data = buf[1..bytesRead];

                switch(type)
                {
                    case PacketType.WelcomePacket:
                    {
                        WelcomePacket<TSState, TCState> packet = BinarySerializer<WelcomePacket<TSState, TCState>>.Deserialize(data, commonState.decompress);
                        mpManager.HandleWelcomePacket(packet);
                        break;
                    }
                    case PacketType.ClientState:
                    {
                        TCState state = mpManager.clientStates[clientId];
                        BinarySerializer<TCState>.DeserializeRef(data, ref state, commonState.decompress);
                        break;
                    }
                    case PacketType.ServerState:
                    {
                        TSState state = mpManager.serverState;
                        BinarySerializer<TSState>.DeserializeRef(data, ref state, commonState.decompress);
                        break;
                    }
                    default: receivePacket?.Invoke(type, data); break;
                }
            }
            catch(Exception exc)
            {
                Out($"{exc.GetType()} in HandleServerCommunication ;; {exc.Message}", ConsoleColor.Red);
            }
        }
    }
}