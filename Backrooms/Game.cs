global using MpManager = Backrooms.Online.MpManager<Backrooms.Online.ServerState, Backrooms.Online.ClientState, Backrooms.Online.Request>;

using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Linq;
using Backrooms.Gui;
using Backrooms.ItemManagement;
using Backrooms.Entities;
using Backrooms.Online;
using Backrooms.Debugging;
using System.Collections.Generic;
using Backrooms.SaveSystem;

namespace Backrooms;

public class Game
{
    public const int GraffitiCount = 1750;


    public readonly Window win;
    public readonly Renderer rend;
    public readonly Camera camera;
    public readonly Input input;
    public readonly EntityManager entityManager;
    public readonly MpManager mpManager;
    public readonly Map map;
    public readonly StartMenu startMenu;
    public readonly CameraController cameraController;
    public readonly Inventory inventory;
    public readonly PlayerStats playerStats;
    public readonly DeathScreen deathScreen;
    public event Action<Vec2f> generateMap;
    public bool playerDead;

    private readonly RoomGenerator generator = new();
    private readonly TextElement debugTextLhs, debugTextRhs;
    private readonly List<ItemWorldObject> worldObjects = [];


    public Game(Window window)
    {
        win = window;
        input = window.input;
        rend = window.renderer;
        camera = rend.camera;

        mpManager = new();
        SetUpMpManager();

        playerStats = new(this);
        playerStats.takeDamage += () => AudioPlayback.PlayOneShot("oof");

        rend.map = map = new(new Tile[0, 0]) {
            texturesStr = [null, null, null, "wall", "pillar"],
            graffitiTexturesStr = Resources.sprites.Where(kvp => kvp.Key.StartsWith("gr_")).Select(kvp => kvp.Key).ToArray(),
            floorTexStr = "carpet",
            ceilTexStr = "ceiling",
            lightTexStr = "light",
            floorTexScale = .1f,
            ceilTexScale = 1f,
            floorLuminance = .5f,
            ceilLuminance = .5f
        };

        cameraController = new(camera, mpManager, window, input, map, rend);

        rend.lightDistribution = new GridLightDistribution(10);
        GenerateMap(RNG.signedInt);
        //rend.lightDistribution = new PointLightDistribution(map.size);
        //(rend.lightDistribution as PointLightDistribution).AddLightSources(
        //    Enumerable.Range(0, 2000)
        //    .Select(i => RNG.Vec2i(map.size))
        //    .Where(pt => map[pt].IsEmpty()));

        ColorBlock invColors = new(Color.Black, 125, 185, 225);
        inventory = new(window, rend, this, input, cameraController, new(5, 2), invColors);
        inventory.AddItem("vodka");
        inventory.AddItem("oli");

        window.console.Add(new(["noclip", "no_clip"], args => window.console.ParseBool(args.FirstOrDefault(), ref cameraController.noClip), "NO_CLIP <enabled>", [0, 1]));

        debugTextLhs = new("lhs", "0 fps", FontFamily.GenericMonospace, 15f, Color.White, Vec2f.zero, Vec2f.zero, Vec2f.zero);
        debugTextRhs = new("rhs", "0 fps", FontFamily.GenericMonospace, 15f, Color.White, Vec2f.right, Vec2f.right, Vec2f.zero);
        rend.guiGroups.Add(new(rend, "debug", true) {
            debugTextLhs,
            debugTextRhs
        });

        window.tick += Tick;

        Atlas atlas = new(map, camera, new(rend.virtRes.y - 32), new(16 + (rend.virtRes.x - rend.virtRes.y) / 2, 16));
        rend.dimensionsChanged += () => {
            atlas.size = new(rend.virtRes.y - 32);
            atlas.loc = new(16 + (rend.virtRes.x - rend.virtRes.y) / 2, 16);
        };
        DepthBufDisplay zBufDisplay = new(rend);
        window.tick += dt => {
            atlas.enabled = input.KeyHelt(Keys.Tab);
            zBufDisplay.enabled = input.KeyHelt(Keys.ControlKey);
        };
        rend.postProcessEffects.Add(atlas);
        rend.postProcessEffects.Add(zBufDisplay);


        //HVDistortion distortion = new(x => {
        //    float strength = 1f - playerStats.sanity;
        //    float fac = strength < 5f ? Utils.Sqr(strength/5f) : MathF.Cbrt(strength/5f);
        //    return Utils.Clamp(MathF.Sin(MathF.Sqrt(MathF.Abs(2f * strength)) * (window.timeElapsed + x)) * fac, -2f, 2f);
        //});
        //renderer.postProcessEffects.Add(distortion);

        entityManager = new(mpManager, window, map, camera, this, rend);
        //entityManager.LoadEntities("Entities");
        //entityManager.types.Find(t => t.tags.instance == "Olaf.Behaviour").Instantiate();
        //foreach(EntityType type in entityManager.types)
        //    type.Instantiate();

        startMenu = new(this, window, rend, camera, cameraController, map, mpManager, entityManager);

        deathScreen = new(cameraController, playerStats, this, rend, win, startMenu);

        SaveManager.Load(SaveFile.Settings);
        DevConsole.windowMode = SaveManager.settings.devConsole ? DevConsole.WindowMode.Restore : DevConsole.WindowMode.Hide;
        startMenu.settingsGui.GetElement<ValueSelectorElement>("resolution").value = SaveManager.settings.resolutionIndex;
        rend.FindGuiGroup("debug").enabled = SaveManager.settings.showDebugInfo;
    }


    public void KillPlayer()
    {
        if(playerDead)
            return;

        playerDead = true;
        inventory.enabled = false;
        win.SetCursor(true);
        deathScreen.Enable();
        cameraController.canMove = false;
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
            map.GenerateGraffitis(GraffitiCount, seed);

            for(int x = 0; x < map.size.x; x++)
                for(int y = 0; y < map.size.y; y++)
                    if(rend.lightDistribution.IsLightTile(new(x, y)))
                        map[x, y] = Tile.Air;

            camPos = map.size/2;
        }

        generate();

        while(camPos.Floor() is Vec2i cPos)
            if(map.InBounds(cPos))
                if(map.IsCollidingTile(cPos))
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
        {
            debugTextLhs.text =
                $"""
                {win.currFps} fps
                {(mpManager.isConnected ? $"Client #{mpManager.clientId}" : "Not connected")}
                Pos: {camera.pos.Floor():$x, $y}
                Map size: {map.size}
                Seed: {generator.seed}
                Entities: {entityManager.instances.Count}
                Sprites: {rend.sprites.Count}
                """;

            debugTextRhs.text =
                $"""
                Health: {playerStats.health:0%}
                Saturation: {playerStats.saturation:0%}
                Hydration: {playerStats.hydration:0%}
                Sanity: {playerStats.sanity:0%}
                """;
        }

        if(input.KeyDown(Keys.F1))
            win.ToggleCursor();

        if(input.KeyDown(Keys.F))
            rend.lighting ^= true;

        if(input.KeyDown(Keys.Escape))
        {
            if(startMenu.startGui.enabled)
                Window.Exit();
            else if(inventory.enabled)
                inventory.enabled = false;
            else
            {
                startMenu.startGui.enabled = true;
                win.SetCursor(true);
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

        if(input.KeyDown(Keys.L))
        {
            map.textures[(int)Tile.Pillar] = map.textures[(int)Tile.Wall] = map.ceilTex = map.floorTex = new("lukas", false);
            map.floorTexScale = 1f;

            UnsafeGraphic table = new("table");

            for(int i = 0; i < 500; i++)
            {
                Vec2i pos = new(RNG.Range(map.size.x), RNG.Range(map.size.y));
                worldObjects.Add(new(rend, win, cameraController, input, inventory, pos + Vec2f.half, table, Item.items["vodka"]));
            }
        }
    }

    private void SetUpMpManager()
    {
        mpManager.connectedToServer += () => {
            GenerateMap(mpManager.serverState.levelSeed);
            camera.pos = mpManager.clientState.pos;

            foreach(KeyValuePair<ushort, ClientState> kvp in mpManager.clientStates.Where(c => c.Key != mpManager.clientId))
                handleClientConnect(kvp.Key);
        };

        mpManager.disconnectedFromServer += () => {
            foreach(KeyValuePair<ushort, ClientState> kvp in mpManager.clientStates.Where(c => c.Key != mpManager.clientId))
            {
                win.tick -= kvp.Value.updaterDelegate;
                rend.sprites.Remove(kvp.Value.renderer);
            }

            mpManager.clientStates.Clear();
            startMenu.startGui.enabled = true;
        };

        mpManager.receiveClientRequest += req => {
            switch(req)
            {
                case Request.GenerateMap:
                    GenerateMap(mpManager.serverState.levelSeed);
                    break;
                default:
                    Out(Log.MpManager, $"Unknown/unhandled client request: {req} ({(int)req})");
                    break;
            }
        };

        void handleClientConnect(ushort id)
        {
            if(id == mpManager.clientId)
                return;

            ClientState state = mpManager.clientStates[id];

            UnsafeGraphic graphic = Resources.graphics[RNG.coinToss ? "walter1" : "walter2"];
            state.renderer = new(state.pos, new Vec2f(graphic.whRatio * .75f, .75f), graphic);
            state.renderer.Ground();
            rend.sprites.Add(state.renderer);

            state.updaterDelegate = _ => state.renderer.pos = state.pos;
            win.tick += state.updaterDelegate;
        }

        mpManager.remoteClientConnected += handleClientConnect;

        mpManager.remoteClientDisconnected += id => {
            if(id == mpManager.clientId)
                return;

            ClientState state = mpManager.clientStates[id];

            rend.sprites.Remove(state.renderer);
            state.renderer = null;

            win.tick -= state.updaterDelegate;
        };
    }
}