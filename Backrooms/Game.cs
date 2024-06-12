using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using Backrooms.Online;
using System.Drawing;
using Backrooms.Pathfinding;
using Backrooms.PostProcessing;
using Backrooms.Gui;
using System.IO;

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
    public float playerSpeed = 2f, sensitivity = 1/5000f;
    public StartMenu startMenu;
    public MpHandler mpHandler;
    public readonly List<(byte id, SpriteRenderer renderer)> playerRenderers = [];
    public readonly Image[] skins = (from str in new string[] { "hazmat_suit", "entity", "freddy_fazbear", "huggy_wuggy", "purple_guy" }
                                    select Resources.sprites[str])
                                    .ToArray();
    public Entity[] customEntities;


    private readonly RoomGenerator generator = new();
    private readonly TextElement fpsDisplay;
    private int fpsCounter;


    public Game(Window window)
    {
        this.window = window;
        renderer = window.renderer;
        input = window.input;

        camera = renderer.camera = new(90f * Utils.Deg2Rad, map.size.length, 270f * Utils.Deg2Rad) {
            maxDist = 20f,
            pos = (Vec2f)map.size/2f,
            fixFisheyeEffect = true
        };

        window.tick += Tick;

        fpsDisplay = new("fps", "0 fps", FontFamily.GenericMonospace, 17.5f, Color.White, Anchor.TopLeft, Vec2f.zero, Vec2f.zero);
        renderer.guiGroups.Add(new(renderer, "hud", true) {
            fpsDisplay
        });
        window.pulse += () => {
            fpsDisplay.text = $"{fpsCounter} fps";
            fpsCounter = 0;
        };

        mpHandler = new(this);
        startMenu = new(window, this, mpHandler);

        renderer.map = map;
        map.texturesStr = [null, "wall", "pillar"];
        map.floorTexStr = "carpet";
        map.ceilTexStr = "ceiling";
        map.floorTexScale = .1f;
        map.ceilTexScale = 3f;
        map.floorLuminance = .5f;
        map.ceilLuminance = .5f;

        //PostProcessEffect lsdEffect = new HVDistortion(x => MathF.Sin(2.5f * (window.timeElapsed + x)) / 20f, x => MathF.Cos(2.5f * (window.timeElapsed + x)) / 20f, enabled: false);
        //renderer.postProcessEffects.Add(lsdEffect);
        //renderer.postProcessEffects.Add(new CrtScreen());
        //renderer.postProcessEffects.Add(new DistanceFog(Renderer.GetDistanceFog, renderer.depthBuf));
        //renderer.postProcessEffects.Add(new VDistortion(x => MathF.Sin(2.5f * (window.timeElapsed + x)) / 20f));
        //renderer.postProcessEffects.Add(new HDistortion(x => MathF.Cos(2.5f * (window.timeElapsed + x)) / 20f));
        //renderer.postProcessEffects.Add(new HVDistortion(x => MathF.Sin(2.5f * (window.timeElapsed + x)) / 20f, x => MathF.Cos(2.5f * (window.timeElapsed + x)) / 20f));
        //window.tick += dt => lsdEffect.enabled ^= input.KeyDown(Keys.L);
        //olafPathfinder = new(map, map.size/2f, olafScholz.size.x/2f, olafSpeed);
        //window.pulse += () => olafPathfinder.RefreshPath(mpHandler.GetClientState(mpHandler.serverState.olafTarget)?.pos ?? map.size/2f);

        customEntities = (from e in Directory.GetFiles("Entities", "*.zip", SearchOption.TopDirectoryOnly)
                          .Concat(Directory.GetDirectories("Entities", "*", SearchOption.TopDirectoryOnly))
                          select new Entity(this, e)).ToArray();

        mpHandler.start += () => {
            mpHandler.onFinishConnect += () => {
                mpHandler.ownClientState.skinIdx = 0;
                mpHandler.SendClientStateChange(StateKey.C_Skin);
                mpHandler.SendClientRequest(RequestKey.C_UpdateSkin);

                foreach(var (id, state) in mpHandler.clientStates)
                    if(id != mpHandler.ownClientId)
                    {
                        Image skin = skins[state.skinIdx];
                        SpriteRenderer newPlayerRenderer = new(state.pos, new((float)skin.Width/skin.Height, 1f), skin);
                        renderer.sprites.Add(newPlayerRenderer);
                        playerRenderers.Add((id, newPlayerRenderer));
                    }
            };

            mpHandler.onPlayerConnect += id => {
                if(id != mpHandler.ownClientId)
                {
                    ClientState state = mpHandler.GetClientState(id);
                    Image skin = skins[state.skinIdx];
                    SpriteRenderer newPlayerRenderer = new(state.pos, new((float)skin.Width/skin.Height, 1f), skin);
                    renderer.sprites.Add(newPlayerRenderer);
                    playerRenderers.Add((id, newPlayerRenderer));
                }
            };
        };
    }


    public void GenerateMap(int seed)
    {
        Out($"Generating map with seed {seed}");

        generator.Initiate(seed);

        generator.GenerateHallways();
        generator.GenerateRooms();
        generator.GeneratePillarRooms();

        map.SetTiles(generator.FormatTiles());

        camera.pos = map.size/2f;
        while(camera.pos.Floor() is Vec2i cPos)
            if(map.InBounds(cPos))
            {
                if(Map.IsCollidingTile(map[cPos]))
                    camera.pos += Vec2f.right;
                else
                    break;
            }
            else
            {
                generator.Initiate();
                generator.GenerateHallways();
                generator.GenerateRooms();
                generator.GeneratePillarRooms();
                map.SetTiles(generator.FormatTiles());
                camera.pos = map.size/2f;
            }
        camera.pos += Vec2f.half;

        if(mpHandler.started)
        {
            mpHandler.ownClientState.pos = camera.pos;
            mpHandler.SendClientStateChange(StateKey.C_Pos);
        }
    }

    public void ReloadSkins()
    {
        foreach(var (id, renderer) in playerRenderers)
            if(id != mpHandler.ownClientId)
                renderer.SetImage(skins[mpHandler.GetClientState(id).skinIdx]);
    }


    private void Tick(float dt)
    {
        //{
        //    CrtScreen crtEffect = renderer.postProcessEffects.Find(p => p is CrtScreen) as CrtScreen;
        //    if(input.KeyHelt(Keys.M)) crtEffect.distortionConstants.x += dt;
        //    if(input.KeyHelt(Keys.N)) crtEffect.distortionConstants.x -= dt;
        //    if(input.KeyHelt(Keys.K)) crtEffect.distortionConstants.y += dt;
        //    if(input.KeyHelt(Keys.J)) crtEffect.distortionConstants.y -= dt;

        //    Out(crtEffect.distortionConstants);
        //}

        fpsCounter++;

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

        Vec2f prevCamPos = camera.pos;
        camera.pos += playerSpeed * dt * (
            (input.KeyHelt(Keys.A) ? 1f : input.KeyHelt(Keys.D) ? -1f : 0f) * camera.right +
            (input.KeyHelt(Keys.S) ? -1f : input.KeyHelt(Keys.W) ? 1f : 0f) * camera.forward).normalized;
        camera.pos = map.ResolveIntersectionIfNecessery(prevCamPos, camera.pos, .25f, out _);
        if(input.lockCursor)
            camera.angle += input.mouseDelta.x * renderer.singleDownscaleFactor * sensitivity / dt;

        if(mpHandler is null || !mpHandler.ready)
            return;

        #region Input
        mpHandler.ownClientState.pos = camera.pos;
        mpHandler.ownClientState.rot = camera.angle;
        mpHandler.SendClientStateChange(StateKey.C_Pos);

        if(!map.InBounds(camera.pos))
            mpHandler.ownClientState.pos = camera.pos = map.size/2f;

        if(input.KeyDown(Keys.F5))
            if(!mpHandler.isHost)
                Out("You must be host to refresh the map!");
            else
            {
                mpHandler.serverState.levelSeed = new Random().Next();
                mpHandler.SendServerStateChange(StateKey.S_LevelSeed);
                Thread.Sleep(1);
                mpHandler.SendServerRequest(RequestKey.S_RegenerateMap);
            }
        #endregion

        foreach(var (id, rend) in playerRenderers)
            rend.pos = mpHandler.GetClientState(id).pos;
    }
}