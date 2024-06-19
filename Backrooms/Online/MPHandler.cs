using System;
using System.Collections.Generic;
using System.IO;
using Backrooms.Online.Generic;

namespace Backrooms.Online;

public class MpHandler
{
    public readonly Game game;

    public readonly List<(byte id, ClientState state)> clientStates = [];
    public readonly ServerState serverState = new();
    public event Action onFinishConnect;
    public event Action<byte> onPlayerConnect;
    public event Action start;

    private string _ipAddress;
    private int _port, _bufSize = 512;
    private bool _printDebug;
    private bool _isHost;


    public byte ownClientId { get; private set; }
    public ClientState ownClientState { get; private set; }
    public bool ready { get; private set; }
    public bool started { get; private set; }
    public string ipAddress
    {
        get => _ipAddress;
        set => SetIfNotStarted(ref _ipAddress, value);
    }
    public int port
    {
        get => _port;
        set => SetIfNotStarted(ref _port, value);
    }
    public int bufSize
    {
        get => _bufSize;
        set => SetIfNotStarted(ref _bufSize, value);
    }
    public bool printDebug
    {
        get => _printDebug;
        set => SetIfNotStarted(ref _printDebug, value);
    }
    public bool isHost
    {
        get => _isHost;
        set => SetIfNotStarted(ref _isHost, value);
    }
    public Server server { get; private set; }
    public Client client { get; private set; }


    public MpHandler(Game game, bool isHost, string ipAddress, int port, int bufSize = 512, bool printDebug = false)
    {
        this.game = game;
        this.isHost = isHost;
        this.ipAddress = ipAddress;
        this.port = port;
        this.printDebug = printDebug;
        this.bufSize = bufSize;
    }

    public MpHandler(Game game, int bufSize = 512, bool printDebug = false)
    {
        this.game = game;
        this.printDebug = printDebug;
        this.bufSize = bufSize;
    }


    public void Start()
    {
        started = true;

        if(isHost)
        {
            server = new(bufSize, printDebug);
            StartHost();
        }

        client = new(bufSize, printDebug);
        StartClient();

        start?.Invoke();
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

    public void SendServerStateChangeAsServer(params StateKey[] keys)
        => server.BroadcastPacket(serverState.Serialize(keys), [ownClientId]);

    public void SendServerRequest(RequestKey key)
        => server.BroadcastPacket([(byte)key]);
    public void SendServerRequest(RequestKey key, byte[] additionalData, byte[] excluded)
        => server.BroadcastPacket([(byte)key, ..additionalData], excluded);

    public void SendClientRequest(RequestKey key)
        => client.SendPacket([(byte)key]);


    private void StartHost()
    {
        server.handlePacket += (client, packet, length) => OutIf(printDebug, "[Server received packet] " + packet[0..length].FormatStr(" ", b => Convert.ToString(b, 16).PadLeft(2, '0')));
        server.constructWelcomePacket += ConstructWelcomePacket;
        server.handleClientRequest += HandleClientRequest;
        server.handleClientState += (clientId, packet, length) => server.BroadcastPacket(packet, [clientId], length);
        server.handleServerState += (packet, length) => server.BroadcastPacket(packet, length);
        server.connect += clientId => {
            ClientState newState = new(clientId) {
                pos = game.map.size/2f
            };

            clientStates.Add((clientId, newState));
            if(clientId != 1)
                onPlayerConnect?.Invoke(clientId);

            server.BroadcastPacket(newState.Serialize(ClientState.allKeys), [clientId, ownClientId]);
        };
        server.disconnect += clientId => {
            clientStates.RemoveAll(c => c.id == clientId);
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
        RequestKey key = (RequestKey)packet[0];
        Out($"Received server request: {key}");

        //switch(key)
        //{
        //    case RequestKey.S_RegenerateMap:
        //        game.GenerateMap(serverState.levelSeed);
        //        break;
        //    case RequestKey.S_UpdateSkin:
        //        game.ReloadSkins();
        //        break;
        //}
    }

    private void HandleClientRequest(byte clientId, byte[] packet, int length)
    {
        RequestKey key = (RequestKey)packet[0];
        Out($"Received client request: {key}, by client #{clientId}");

        switch(key)
        {
            case RequestKey.C_UpdateSkin:
                SendServerRequest(RequestKey.S_UpdateSkin);
                break;
        }
    }

    /// <summary>
    /// Layout of serialized welcome packet ([..] == 1 byte, {..} (* x) == x times, *** == Already implemented in surrounding statement): <br />
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
            GetClientState(data[(int)stream.Position + 1]).Deserialize(data, (int)stream.Position, null, out int csBytesRead);
            stream.Position += csBytesRead;
        }

        ownClientState = GetClientState(ownClientId);

        ready = true;
        onFinishConnect?.Invoke();

        OutIf(printDebug, $"[Client] Successfully handled welcome packet ({length} bytes)");
    }

    private void SetIfNotStarted<T>(ref T field, T value)
        => field = !started ? value : throw new InvalidOperationException($"Cannot change this value when client/server has already started");
}