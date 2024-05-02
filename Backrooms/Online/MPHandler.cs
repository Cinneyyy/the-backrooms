using System;
using System.Collections.Generic;
using System.IO;
using Backrooms.Online.Generic;

namespace Backrooms.Online;

public class MPHandler(Game game, bool isHost, string ipAddress, int port, int bufSize = 256, bool printDebug = false)
{
    public readonly Game game = game;
    public readonly bool isHost = isHost;

    public readonly string ipAddress = ipAddress;
    public readonly int port = port;
    public readonly Server server = isHost ? new(bufSize, printDebug) : null;
    public readonly Client client = new(bufSize, printDebug);
    public readonly ServerState serverState = new();
    public readonly List<(byte id, ClientState state)> clientStates = [];
    public event Action onFinishConnect;
    public event Action<byte> onPlayerConnect;


    public byte ownClientId { get; private set; }
    public ClientState ownClientState { get; private set; }
    public bool ready { get; private set; }


    public void Start()
    {
        if(isHost) 
            StartHost();

        StartClient();
    }

    public ClientState GetClientState(byte clientId)
    {
        ClientState state = clientStates.Find(c => c.id == clientId).state;

        if(state is null)
        {
            state = new(clientId);
            clientStates.Add((clientId, state));

            if(ready)
                onPlayerConnect?.Invoke(clientId);
        }

        return state;
    }

    public void SendClientStateChange(params StateKey[] keys)
        => client.SendPacket(ownClientState.Serialize(keys));

    public void SendServerStateChange(params StateKey[] keys)
        => client.SendPacket(serverState.Serialize(keys));


    private void StartHost()
    {
        server.handlePacket += (client, packet, length) => OutIf(printDebug, "[Server received packet] " + packet[0..length].FormatStr(" ", b => Convert.ToString(b, 16).PadLeft(2, '0')));
        server.constructWelcomePacket += ConstructWelcomePacket;
        server.handleClientRequest += HandleClientRequest;
        server.handleClientState += (clientId, packet, length) => server.BroadcastPacket(packet, [clientId], length);
        server.handleServerState += server.BroadcastPacket;
        server.connect += clientId => {
            ClientState newState = new(clientId) {
                pos = game.map.size/2f
            };

            clientStates.Add((clientId, newState));
            if(clientId != 1)
                onPlayerConnect?.Invoke(clientId);

            server.BroadcastPacket(newState.Serialize(ClientState.allKeys), [clientId, ownClientId]);
        };

        server.StartHosting(port);
    }

    private void StartClient()
    {
        client.handlePacket += (packet, length) => OutIf(printDebug, "[Client received packet] " + packet[0..length].FormatStr(" ", b => Convert.ToString(b, 16).PadLeft(2, '0')));
        client.handleWelcomePacket += HandleWelcomePacket;
        client.handleClientState += (clientId, packet, length) => GetClientState(clientId).Deserialize(packet, 0, length, out _);
        client.handleServerState += (packet, length) => serverState.Deserialize(packet, 0, length, out _);
        client.handleServerRequest += HandleServerRequest;

        client.Connect(ipAddress, port);
    }

    private void HandleServerRequest(byte[] packet, int length)
    {

    }

    private void HandleClientRequest(byte clientId, byte[] packet, int length)
    {

    }

    /// <summary>
    /// Layout of serialized welcome packet ([..] == 1 byte, {..} == x times, *** == Already implemented in surrounding statement): <br />
    /// <code>
    /// [PacketType.WelcomePacket as byte]
    /// [Own client ID]
    /// [data] (* x, server state)
    /// ***[PacketType.EndOfData as byte]
    /// {
    ///     ***[PacketType.ClientState]
    ///     ***[client ID]
    ///     [data] (* x, each client field)
    ///     ***[PacketType.EndOfData as byte]
    /// } (* amount of previously connected clients)
    /// </code>
    /// </summary>
    private byte[] ConstructWelcomePacket(byte clientId)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write((byte)PacketType.WelcomePacket);
        writer.Write(clientId);
        writer.Write(serverState.Serialize(ServerState.allKeys));

        foreach(var (_, state) in clientStates)
            writer.Write(state.Serialize(ClientState.allKeys));

        OutIf(printDebug, $"[Server] Constructed welcome packet of size {stream.Length} [bytes]");
        return stream.ToArray();
    }

    private void HandleWelcomePacket(byte[] data, int length)
    {
        using MemoryStream stream = new(data, 0, length);
        using BinaryReader reader = new(stream);

        reader.ReadByte();

        ownClientId = reader.ReadByte();

        serverState.Deserialize(data, 2, null, out int ssBytesRead);
        stream.Position += ssBytesRead;

        while(stream.Position < stream.Length)
        {
            GetClientState(ownClientId).Deserialize(data, (int)stream.Position, null, out int csBytesRead);
            stream.Position += csBytesRead;
        }

        ownClientState = GetClientState(ownClientId);

        ready = true;
        onFinishConnect?.Invoke();

        OutIf(printDebug, $"[Client] Successfully handled welcome packet ({length} bytes)");
    }
}