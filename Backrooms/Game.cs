using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Backrooms.Online;

namespace Backrooms;

public class Game
{
    public Window window;
    public Renderer renderer;
    public Camera camera;
    public Input input;
    public Map map = new(new byte[,] {
        { 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 0, 1, 0, 0, 0, 0, 1 },
        { 1, 0, 1, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 2, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1 },
    });
    public float playerSpeed = 2f, sensitivity = 1f;
    public SpriteRenderer olafScholz;
    public AudioSource olafScholzAudio;
    public float olafSpeed = .75f;
    public bool isHost;
    public List<(ClientState client, byte id)> clientStates = [];
    public List<(SpriteRenderer renderer, byte id)> playerRenderers = [];
    public ServerState serverState;
    public Client client;
    public Server server; // Null if not host
    public byte ownClientId;

    private readonly RoomGenerator generator = new();
    //private readonly Timer fpsTimer = new();


    public ClientState ownClientState => clientStates.Find(c => c.id == ownClientId).client;


    public Game(Window window, bool host)
    {
        this.window = window;
        renderer = window.renderer;
        input = window.input;

        camera = renderer.camera = new(90f * Utils.Deg2Rad, map.size.length);
        camera.pos = (Vec2f)map.size/2f;
        camera.angle = 270f * Utils.Deg2Rad;

        window.tick += Tick;

        //fpsTimer.Tick += (_, _) => Out(1f / window.deltaTime);
        //fpsTimer.Interval = 1000;
        //fpsTimer.Start();

        renderer.map = map;
        map.textures = [
            null,
            new LockedBitmap(Resources.sprites["wall"], PixelFormat.Format24bppRgb),
            new LockedBitmap(Resources.sprites["pillar"], PixelFormat.Format24bppRgb)
        ];

        renderer.sprites.Add(olafScholz = new(camera.pos, new(.8f), true, Resources.sprites["oli"]));
        olafScholzAudio = new(Resources.audios["scholz_speech_1"]) {
            loop = true
        };
        olafScholzAudio.Play();

        const string ip = "127.0.0.1";
        const int port = 1234;

        serverState = new() {
            levelSeed = 0,
            olafPos = map.size/2f,
            olafTarget = 1
        };

        if(host)
        {
            server = new(true);
            //server.handleClientRequest += (clientId, packet, packetSize) => { };
            server.connect += clientId => {
                // Inform new client of their own id
                server.SendPacket(clientId, [(byte)RequestKey.S_SetOwnId, clientId, 0]);

                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                writer.Write((byte)RequestKey.S_SetClientList);

                foreach(var (client, id) in clientStates)
                {
                    writer.Write(id);
                    writer.Write(client.Serialize(id, ClientState.allKeys));
                }

                // Send new client a list of all other clients
                server.SendPacket(clientId, stream.ToArray());

                // Send current server state
                server.SendPacket(clientId, [(byte)RequestKey.S_SetServerState, ..serverState.Serialize(null, ServerState.allKeys)]);

                // Broadcast to every client (including the new one, since they have the old list) that a new one has joined
                server.BroadcastPacket([(byte)RequestKey.S_PlayerJoined, clientId, 0]);
            };
            //server.handlePacket += (clientId, packet, packetSize) => { };
            //server.handleClientState += (clientId, packet, packetSize) => { };
            server.handleClientState += (clientId, packet, packetSize) => server.BroadcastPacket(packet, clientId, packetSize);
            server.StartHosting(port);
        }
        
        client = new(true);
        void receiveClientListCheck(byte[] packet, int packetSize)
        {
            if(packet[0] != (byte)RequestKey.S_SetClientList)
                return;

            using MemoryStream stream = new(packet, 1, packetSize-1);
            using BinaryReader reader = new(stream);

            List<byte> currState = [];
            byte currId = 0;

            while(reader.BaseStream.Position < reader.BaseStream.Length)
            {
                if(currId != 0)
                {
                    ClientState newState = new();
                    (newState as IState<StateKey>).Deserialize([.. currState], 0, currState.Count);
                    clientStates.Add((newState, currId));
                    playerRenderers.Add((new(Vec2f.zero, new(.25f, .5f), false, Resources.sprites["square"]), currId));
                }

                currId = reader.ReadByte();
                currState.Clear();
                while(reader.ReadByte() is byte next && next != 0)
                    currState.Add(next);
            }

            client.handleServerRequest -= receiveClientListCheck;
        }
        client.handleServerRequest += receiveClientListCheck;
        client.handleServerRequest += (packet, packetSize) => {
            switch((RequestKey)packet[0])
            {
                case RequestKey.S_SetOwnId: 
                    ownClientId = packet[1]; 
                    break;
                case RequestKey.S_PlayerJoined:
                    clientStates.Add((new(), packet[1]));
                    if(packet[1] != ownClientId)
                    {
                        SpriteRenderer playerRenderer = new(Vec2f.zero, new(.25f, .5f), false, Resources.sprites["square"]);
                        playerRenderers.Add((playerRenderer, packet[1]));
                        renderer.sprites.Add(playerRenderer);
                    }
                    break;
                case RequestKey.S_SetServerState:
                    (serverState as IState<StateKey>).Deserialize(packet, 1, packetSize);
                    break;
            }
        };
        //client.handlePacket += (packet, packetSize) => { };
        client.handleServerState += (packet, packetSize) => serverState.Deserialize(packet, 0, packetSize);
        client.handleClientState += (clientId, packet, packetSize) => clientStates.Find(c => c.id == clientId).client?.Deserialize(packet, 0, packetSize);
        client.Connect(ip, port);
    }


    public void GenerateMap()
    {
        Console.WriteLine("Initiating...");
        generator.Initiate();

        Console.WriteLine("Generating rooms & hallways...");
        generator.GenerateHallways();
        generator.GenerateRooms();
        generator.GeneratePillarRooms();

        //Console.WriteLine("Building Walls...");
        //List<(Vec2i from, Vec2i to)> lines = [];
        //using Bitmap walls = new(generator.gridSize.w*8+2, generator.gridSize.h*8+2);
        //Graphics g = Graphics.FromImage(walls);
        //for(int x = 0; x < generator.gridSize.w; x++)
        //    for(int y = 0; y < generator.gridSize.h; y++)
        //    {
        //        if(generator[x, y])
        //        {
        //            g.FillRectangle(Brushes.White, new(x*8+1, y*8+1, 8, 8));

        //            if(x > 0 && !generator[x-1, y]) lines.Add((new(x*8+1, y*8+9), new(x*8+1, y*8)));
        //            if(y > 0 && !generator[x, y-1]) lines.Add((new(x*8+9, y*8+1), new(x*8+1, y*8+1)));
        //            if(x < generator.gridSize.w-1 && !generator[x+1, y]) lines.Add((new(x*8+9, y*8+9), new(x*8+9, y*8)));
        //            if(y < generator.gridSize.h-1 && !generator[x, y+1]) lines.Add((new(x*8+9, y*8+9), new(x*8, y*8+9)));
        //        }
        //    }
        //Pen pen = new(Brushes.White);

        //g.Clear(Color.Black);
        //foreach(var ln in lines)
        //    g.DrawLine(pen, ln.from, ln.to);
        //g.Dispose();
        //using LockedBitmap lb = new(walls, PixelFormat.Format24bppRgb);

        //Console.WriteLine("Converting to sensible format...");
        //Tile[,] tiles = new Tile[walls.Width, walls.Height];
        //for(int x = 0; x < walls.Width; x++)
        //    for(int y = 0; y < walls.Height; y++)
        //        tiles[x, y] = lb.GetPixel24(x, y).r < 0x7f ? Tile.Empty : Tile.Wall;

        Console.WriteLine("Refreshing map...");
        map.SetTiles(generator.FormatTiles());

        Console.WriteLine("Moving player...");
        camera.pos = map.size/2f;
        while(camera.pos.Floor() is Vec2i cPos)
            if(map.InBounds(cPos))
            {
                if(map[cPos] != Tile.Empty)
                    camera.pos += Vec2f.right;
                else
                    break;
            }
            else
            {
                Console.WriteLine("Regenerating...");
                generator.Initiate();
                generator.GenerateHallways();
                generator.GenerateRooms();
                generator.GeneratePillarRooms();
                map.SetTiles(generator.FormatTiles());
                camera.pos = map.size/2f;
            }
        camera.pos += Vec2f.half;
        camera.maxDist = 50f;

        Console.WriteLine("Finished!");
    }


    private void Tick(float dt)
    {
        #region Input
        Vec2f prevCamPos = camera.pos;
        camera.pos += playerSpeed * dt * (
            (input.KeyHelt(Keys.A) ? 1f : input.KeyHelt(Keys.D) ? -1f : 0f) * camera.right +
            (input.KeyHelt(Keys.S) ? -1f : input.KeyHelt(Keys.W) ? 1f : 0f) * camera.forward).normalized;
        camera.pos = map.ResolveIntersectionIfNecessery(prevCamPos, camera.pos, .25f, out _);
        camera.angle += input.mouseDelta.x * renderer.downscaleFactor * sensitivity * dt;
        if(ownClientState is var ownClient && ownClient is not null) 
            ownClient.pos = camera.pos;

        if(!map.InBounds(camera.pos))
            camera.pos = map.size/2f;

        if(input.KeyDown(Keys.F5))
        {
            GenerateMap();
            renderer.sprites[0].pos = camera.pos;
        }

        if(input.KeyDown(Keys.F1))
            input.lockCursor ^= true;

        if(input.KeyDown(Keys.Escape))
            Environment.Exit(0);

        if(input.KeyDown(Keys.F3))
            Debugger.Break();
        #endregion

        Vec2f olafTarget = clientStates.Find(c => c.id == serverState.olafTarget).client?.pos ?? map.size/2f;
        Vec2f olafToPlayer = olafTarget - olafScholz.pos;
        Vec2f oldOlaf = olafScholz.pos;
        if(olafTarget != olafScholz.pos)
            olafScholz.pos += olafToPlayer.normalized * olafSpeed * dt;
        olafScholz.pos = map.ResolveIntersectionIfNecessery(oldOlaf, olafScholz.pos, olafScholz.size.x/2f, out _);
        olafScholzAudio.volume = MathF.Pow(1f - olafToPlayer.length / 10f, 3f);
        serverState.olafPos = olafScholz.pos;

        if(isHost)
        {
            foreach(var (state, id) in clientStates)
            {
                if(id == ownClientId)
                    continue;

                server.SendPacket(id, state.Serialize(id, StateKey.C_Pos));
            }

            server.BroadcastPacket(serverState.Serialize(null, StateKey.S_OlafPos, StateKey.S_OlafTarget), ownClientId);
        }
        else
        {
            byte[] packet = ownClientState?.Serialize(ownClientId, StateKey.C_Pos);

            if(packet is not null)
                client.SendPacket(packet);
        }

        foreach(var (state, id) in clientStates)
        {
            if(id != ownClientId)
                playerRenderers.Find(p => p.id == id).renderer.pos = state.pos;
        }

        olafScholz.pos = serverState.olafPos;
    }
}