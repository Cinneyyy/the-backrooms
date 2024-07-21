global using MpManager = Backrooms.Online.MpManager<Backrooms.Online.ServerState, Backrooms.Online.ClientState, Backrooms.Online.Request>;

using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Linq;
using Backrooms.Gui;
using Backrooms.ItemManagement;
using Backrooms.InputSystem;
using Backrooms.Entities;
using Backrooms.Online;
using Backrooms.Debugging;

namespace Backrooms;

public class Game
{
    public Window window;
    public Renderer renderer;
    public Camera camera;
    public Input input;
    public EntityManager entityManager;
    public MpManager mpManager;
    public Map map;
    public StartMenu startMenu;
    public CameraController cameraController;
    public Inventory inventory;
    public PlayerStats playerStats;
    public event Action<Vec2f> generateMap;

    private readonly RoomGenerator generator = new();
    private readonly TextElement debugTextLhs;
    private readonly Atlas atlas;


    public Game(Window window)
    {
        this.window = window;
        renderer = window.renderer;
        input = window.input;

        mpManager = new();
        SetUpMpManager();

        playerStats = new(100f, 100f, 100f, 100f);

        ColorBlock invColors = new(Color.Black, 125, 185, 225);
        inventory = new(window, renderer, this, input, new(5, 2), invColors);
        inventory.AddItem("vodka");
        inventory.AddItem("oli");

        renderer.map = map = new(new Tile[0, 0]) {
            texturesStr = [null, null, null, "wall", "pillar"],
            floorTexStr = "carpet",
            ceilTexStr = "ceiling",
            floorTexScale = .1f,
            ceilTexScale = 3f,
            floorLuminance = .5f,
            ceilLuminance = .5f
        };

        renderer.camera = camera = new(90f, 20f, 0f);
        cameraController = new(camera, mpManager, window, input, map, renderer);

        GenerateMap(RNG.signedInt);

        startMenu = new(window, renderer, camera, cameraController, map, mpManager);

        window.console.Add(new(["noclip", "no_clip"], args => window.console.ParseBool(args.FirstOrDefault(), ref cameraController.noClip), "NO_CLIP <enabled>", [0, 1]));

        debugTextLhs = new("lhs", "0 fps", FontFamily.GenericMonospace, 15f, Color.White, Vec2f.zero, Vec2f.zero, Vec2f.zero);
        renderer.guiGroups.Add(new(renderer, "debug", true) {
            debugTextLhs
        });

        window.tick += Tick;

        atlas = new(map, camera, new(renderer.virtRes.y - 32), new(16 + (renderer.virtRes.x - renderer.virtRes.y) / 2, 16));
        renderer.dimensionsChanged += () => {
            atlas.size = new(renderer.virtRes.y - 32);
            atlas.loc = new(16 + (renderer.virtRes.x - renderer.virtRes.y) / 2, 16);
        };
        window.tick += dt => atlas.enabled = input.KeyHelt(Keys.Tab);
        renderer.postProcessEffects.Add(atlas);

        //entityManager = new(mpManager, window, map, camera, this, renderer);
        //entityManager.LoadEntities("Entities");

        ItemWorldObject itemTest = new(renderer, window, cameraController, input, new(93f, 93f), new("table"), new("vodka"));
    }


    public void GenerateMap(int seed)
    {
        Vec2f camPos;

        void generate()
        {
            generator.Initiate(seed);
            generator.GenerateHallways();
            generator.GenerateRooms();
            generator.GeneratePillarRooms();

            map.SetTiles(generator.FormatTiles());
            camPos = map.size/2f;
        }

        generate();

        while(camPos.Floor() is Vec2i cPos)
            if(map.InBounds(cPos))
                if(Map.IsCollidingTile(map[cPos]))
                    camPos += Vec2f.right;
                else
                    break;
            else
            {
                seed++;
                generate();
            }

        Out(Log.GameEvent, $"Generated map with seed {seed}");
        Vec2f center = camPos + Vec2f.half;
        cameraController.pos = center;
        generateMap?.Invoke(center);
    }


    private void Tick(float dt)
    {
        if(debugTextLhs.enabled)
            debugTextLhs.text =
                $"""
                {window.currFps} fps
                {(mpManager.isConnected ? $"Client #{mpManager.clientId}" : "Not connected")}
                Pos: {camera.pos.Floor():$x, $y}
                Map size: {map.size}
                Seed: {generator.seed}
                """;

        if(input.KeyDown(Keys.F1))
            window.ToggleCursor();

        if(input.KeyDown(Keys.Escape))
        {
            if(startMenu.startGui.enabled)
                Window.Exit();
            else if(inventory.enabled)
                inventory.enabled = false;
            else
            {
                startMenu.startGui.enabled = true;
                window.SetCursor(true);
            }
        }

        if(input.KeyDown(Keys.F3))
            Debugger.Break();

        if(input.KeyDown(Keys.C))
            DevConsole.Restore();

        if(input.KeyDown(Keys.F5))
        {
            int seed = RNG.signedInt;
            GenerateMap(seed);
            mpManager.serverState.levelSeed = seed;
            mpManager.SyncServerState("levelSeed");
            Thread.Sleep(1);
            mpManager.SendClientReq(Request.GenerateMap);
        }
    }

    private void SetUpMpManager()
    {
        mpManager.connectedToServer += () => {
            GenerateMap(mpManager.serverState.levelSeed);
            camera.pos = mpManager.clientState.pos;
        };

        mpManager.receiveClientRequest += req => {
            switch(req)
            {
                case Request.GenerateMap:
                    GenerateMap(mpManager.serverState.levelSeed);
                    break;
                default:
                    break;
            }
        };

        mpManager.clientConnected += id => {
            if(id == mpManager.clientId)
                return;

            ClientState state = mpManager.clientStates[id];

            state.renderer = new(state.pos, new Vec2f(.4f, .9f), new UnsafeGraphic("hazmat_suit"));
            renderer.sprites.Add(state.renderer);

            state.updaterDelegate = _ => state.renderer.pos = state.pos;
            window.tick += state.updaterDelegate;
        };

        mpManager.clientDisconnected += id => {
            if(id == mpManager.clientId)
                return;

            ClientState state = mpManager.clientStates[id];

            renderer.sprites.Remove(state.renderer);
            state.renderer = null;

            window.tick -= state.updaterDelegate;
        };
    }
}