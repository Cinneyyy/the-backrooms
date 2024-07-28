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

namespace Backrooms;

public class Game
{
    public readonly Window win;
    public readonly Renderer rend;
    public readonly Camera camera;
    public readonly Input input;
    public readonly AudioManager audioManager;
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

        audioManager = new(win);

        mpManager = new();
        SetUpMpManager();

        playerStats = new(this);
        playerStats.takeDamage += () => audioManager.PlayOneShot("oof");

        rend.map = map = new(new Tile[0, 0]) {
            texturesStr = [null, null, null, "wall", "pillar"],
            floorTexStr = "carpet",
            ceilTexStr = "ceiling",
            floorTexScale = .1f,
            ceilTexScale = 1f,
            floorLuminance = .5f,
            ceilLuminance = .5f
        };

        cameraController = new(camera, mpManager, window, input, map, rend);
        GenerateMap(RNG.signedInt);

        ColorBlock invColors = new(Color.Black, 125, 185, 225);
        inventory = new(window, rend, this, input, cameraController, new(5, 2), invColors);
        inventory.AddItem("vodka");
        inventory.AddItem("oli");

        startMenu = new(window, rend, camera, cameraController, map, mpManager);

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

        deathScreen = new(cameraController, playerStats, this, rend, win, startMenu);

        //HVDistortion distortion = new(x => {
        //    float strength = 1f - playerStats.sanity;
        //    float fac = strength < 5f ? Utils.Sqr(strength/5f) : MathF.Cbrt(strength/5f);
        //    return Utils.Clamp(MathF.Sin(MathF.Sqrt(MathF.Abs(2f * strength)) * (window.timeElapsed + x)) * fac, -2f, 2f);
        //});
        //renderer.postProcessEffects.Add(distortion);

        entityManager = new(mpManager, window, map, camera, this, rend);
        entityManager.LoadEntities("Entities");
        //foreach(EntityType type in entityManager.types)
        //    type.Instantiate();

        win.tick += dt => map.ceilTexScale += dt * Utils.ToTernary(input, Keys.Down, Keys.Up);
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
            rend.sprites.Add(state.renderer);

            state.updaterDelegate = _ => state.renderer.pos = state.pos;
            win.tick += state.updaterDelegate;
        };

        mpManager.clientDisconnected += id => {
            if(id == mpManager.clientId)
                return;

            ClientState state = mpManager.clientStates[id];

            rend.sprites.Remove(state.renderer);
            state.renderer = null;

            win.tick -= state.updaterDelegate;
        };
    }
}