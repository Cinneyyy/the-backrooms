using System;
using System.Net.Sockets;
using System.Threading;
using Backrooms.Serialization;

namespace Backrooms.Online;

public class Client<TSState, TCState, TReq>(MpManager<TSState, TCState, TReq> mpManager)
    where TSState : Packet<TSState>, new()
    where TCState : Packet<TCState>, new()
    where TReq : struct, Enum
{
    public delegate void ReceivePacketHandler(PacketType type, byte[] data);


    public readonly CommonState commonState = mpManager.commonState;
    public readonly MpManager<TSState, TCState, TReq> mpManager = mpManager;
    public event ReceivePacketHandler handleMiscPacket;
    public event MpManager<TSState, TCState, TReq>.RequestHandler receiveRequest;
    public event MpManager<TSState, TCState, TReq>.ClientEvent clientConnected, clientDisconnected;

    private TcpClient remoteHost;
    private bool receiveInput;


    public bool isConnected { get; private set; }
    public ushort clientId { get; private set; }
    public string remoteIpAddress { get; private set; }
    public int remotePort { get; private set; }
    public long outgoingPacketData { get; private set; }
    public long incomingPacketData { get; private set; }


    public void Connect(string ipAddress, int port)
    {
        try
        {
            if(isConnected)
                throw new($"Client is already connected");

            remoteIpAddress = ipAddress;
            remotePort = port;
            receiveInput = true;

            remoteHost = new(ipAddress, port);

            new Thread(HandleServerCommunication).Start();

            Out(Log.Client, $"Connected to remote server at {ipAddress}:{port}");
        }
        catch(Exception exc)
        {
            OutErr(Log.Client, exc, $"Failed to connect to server ({ipAddress}:{port}) ;; $e");
        }
    }

    public void Disconnect()
    {
        if(!isConnected)
        {
            Out(Log.Client, $"Cannot disconnect while client is not connected to a remote server");
            return;
        }

        isConnected = false;
        receiveInput = false;
        remoteHost.Close();

        Out(Log.Client, $"Disconnected from server at {remoteIpAddress}:{remotePort}");

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

            // Sorry future me for the absolute byte-concatination abuse going on here
            data = [(byte)type, ..(type == PacketType.ClientState ? clientId.ToTwoBytes() : []), ..(prepend ?? []), ..data];
            remoteHost.GetStream().Write(data, 0, data.Length);

            outgoingPacketData += data.Length;
            OutIf(Log.Client, commonState.logPackets, $"Sent {data.Length} byte long packet to server, packet type: {type} // {typeof(T)}");
        }
        catch(Exception exc)
        {
            OutErr(Log.Client, exc, $"{exc.GetType()} in Client.SendPacket ;; $e");
        }
    }

    public void SendPacketRaw(byte[] data)
    {
        try
        {
            remoteHost.GetStream().Write(data, 0, data.Length);

            outgoingPacketData += data.Length;
            OutIf(Log.Client, commonState.logPackets, $"Sent {data.Length} byte long raw packet to server");
        }
        catch(Exception exc)
        {
            OutErr(Log.Client, exc, $"{exc.GetType()} in Client.SendPacketRaw ;; $e");
        }
    }

    public void SendStateChanges(bool serverState, params string[] members)
    {
        if(serverState)
            SendPacket(PacketType.ServerState, mpManager.serverState, members);
        else
            SendPacket(PacketType.ClientState, mpManager.clientState, members);
    }


    private void HandleServerCommunication()
    {
        NetworkStream stream = remoteHost.GetStream();
        byte[] buf = new byte[commonState.bufSize];

        try
        {
            while(receiveInput && stream.Read(buf, 0, buf.Length) is int bytesRead && bytesRead > 0)
                try
                {
                    PacketType type = (PacketType)buf[0];
                    incomingPacketData += bytesRead;

                    if(!mpManager.isConnected && type != PacketType.WelcomePacket)
                    {
                        Out(Log.Client, $"Discarding packet of type {type}, as this client has not yet received welcome packet");
                        continue;
                    }

                    byte[] data = buf[1..bytesRead];
                    OutIf(Log.Client, commonState.logPackets, $"Received server packet with size {bytesRead} bytes and of type {type}");

                    switch(type)
                    {
                        case PacketType.WelcomePacket:
                        {
                            WelcomePacket<TSState, TCState> packet = BinarySerializer<WelcomePacket<TSState, TCState>>.Deserialize(data, commonState.decompress);
                            clientId = packet.clientId;
                            mpManager.HandleWelcomePacket(packet);
                            break;
                        }
                        case PacketType.ClientState:
                        {
                            ushort targetId = data[..2].ToUint16();
                            TCState state = mpManager.clientStates[targetId];
                            BinarySerializer<TCState>.DeserializeRef(data[2..], ref state, commonState.decompress);
                            break;
                        }
                        case PacketType.ServerState:
                        {
                            TSState state = mpManager.serverState;
                            BinarySerializer<TSState>.DeserializeRef(data, ref state, commonState.decompress);
                            break;
                        }
                        case PacketType.ClientReq:
                        {
                            TReq req = PrimitiveSerializer.Deserialize<TReq>(data);
                            receiveRequest?.Invoke(req);
                            break;
                        }
                        case PacketType.IntegrateClient:
                        {
                            IntegrationPacket<TCState> packet = BinarySerializer<IntegrationPacket<TCState>>.Deserialize(data, commonState.decompress);
                            mpManager.clientStates[packet.id] = packet.state;
                            clientConnected?.Invoke(packet.id);
                            Out(Log.Client, $"Integrated client: {packet}");
                            break;
                        }
                        case PacketType.RemoveClient:
                        {
                            ushort id = BinarySerializer<ClientMetaPacket>.Deserialize(data, commonState.decompress).clientId;
                            clientDisconnected?.Invoke(id);
                            mpManager.clientStates.Remove(id);
                            Out(Log.Client, $"Client disconnected: {id}");
                            break;
                        }
                        case PacketType.Misc:
                            handleMiscPacket?.Invoke(type, data);
                            break;
                        default:
                            throw new($"Invalid packet type for client to receive: {type}");
                    }
                }
                catch(Exception exc)
                {
                    OutErr(Log.Client, exc, $"{exc.GetType()} in HandleServerCommunication ;; $e");
                }
        }
        catch(Exception exc)
        {
            OutErr(Log.Client, exc, $"{exc.GetType()} in HandleServerCommunication ;; $e");
        }
        finally
        {
            Disconnect();
            Out(Log.Client, $"Disconnected from server");
        }
    }
}