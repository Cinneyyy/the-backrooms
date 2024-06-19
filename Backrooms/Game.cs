using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using Backrooms.Online;
using System.Drawing;
using Backrooms.Gui;

namespace Backrooms;

public class Game
{
    public Window window;
    public Renderer renderer;
    public Camera camera;
    public Input input;
    public EntityManager entityManager;
    public MpHandler mpHandler;
    public Map map;
    public StartMenu startMenu;
    public CameraController cameraController;

    private readonly RoomGenerator generator = new();
    private readonly TextElement fpsDisplay;


    public Game(Window window)
    {
        this.window = window;
        renderer = window.renderer;
        input = window.input;

        mpHandler = new(this);

        map = new(new Tile[0, 0]) {
            texturesStr = [null, "wall", "pillar"],
            floorTexStr = "carpet",
            ceilTexStr = "ceiling",
            floorTexScale = .1f,
            ceilTexScale = 3f,
            floorLuminance = .5f,
            ceilLuminance = .5f
        };
        renderer.map = map;

        camera = renderer.camera = new(90f * Utils.Deg2Rad, 20f, 0f);
        cameraController = new(camera, mpHandler, window, input, map, renderer);

        GenerateMap(RNG.int32);

        startMenu = new(window, renderer, camera, map, mpHandler);

        fpsDisplay = new("fps", "0 fps", FontFamily.GenericMonospace, 17.5f, Color.White, Vec2f.zero, Vec2f.zero, Vec2f.zero);
        renderer.guiGroups.Add(new(renderer, "fps", true) { fpsDisplay });

        window.tick += Tick;

        entityManager = new(mpHandler, window, map, camera, this);
        entityManager.LoadEntities("Entities");
    }


    public void GenerateMap(int seed)
    {
        Out($"Generating map with seed {seed}");
        Vec2f camPos;

        void generate(int s)
        {
            generator.Initiate(s);
            generator.GenerateHallways();
            generator.GenerateRooms();
            generator.GeneratePillarRooms();

            map.SetTiles(generator.FormatTiles());  
            camPos = map.size/2f;
        }

        generate(seed);

        while(camPos.Floor() is Vec2i cPos)
            if(map.InBounds(cPos))
            {
                if(Map.IsCollidingTile(map[cPos]))
                    camPos += Vec2f.right;
                else
                    break;
            }
            else
                generate(++seed);

        cameraController.pos = camPos + Vec2f.half;
    }


    private void Tick(float dt)
    {
        if(fpsDisplay.enabled)
            fpsDisplay.text = $"{window.currFps} fps";

        if(input.KeyDown(Keys.F1))
            input.lockCursor ^= true;

        if(input.KeyDown(Keys.Escape))
            Window.Exit();

        if(input.KeyDown(Keys.F3))
            Debugger.Break();

        if(input.KeyDown(Keys.F))
            camera.fixFisheyeEffect ^= true;

        if(input.KeyDown(Keys.C))
            DevConsole.Restore();

        if(mpHandler is { ready: true } && input.KeyDown(Keys.F5))
            if(!mpHandler.isHost)
                Out("You must be host to refresh the map!");
            else
            {
                mpHandler.serverState.levelSeed = new Random().Next();
                mpHandler.SendServerStateChange(StateKey.S_LevelSeed);
                Thread.Sleep(1);
                mpHandler.SendServerRequest(RequestKey.S_RegenerateMap);
            }
    }
}