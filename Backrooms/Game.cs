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
    public SpriteRenderer olafScholz;
    public AudioSource olafScholzAudio;
    public float olafSpeed = .75f;
    public MPHandler mpHandler;
    public readonly List<(byte id, SpriteRenderer renderer)> playerRenderers = [];
    public PathfindingEntity olafPathfinder;
    public readonly Image[] skins = (from str in new string[] { "hazmat_suit", "entity", "freddy_fazbear", "huggy_wuggy", "purple_guy" }
                                    select Resources.sprites[str])
                                    .ToArray();
    public Pathfinder olafPathfinding;


    private readonly RoomGenerator generator = new();
    private readonly TextElement fpsDisplay;
    private int fpsCounter;


    public Game(Window window, bool host, string ip, int port, int skinIdx)
    {
        this.window = window;
        renderer = window.renderer;
        input = window.input;

        camera = renderer.camera = new(90f * Utils.Deg2Rad, map.size.length);
        camera.pos = (Vec2f)map.size/2f;
        camera.angle = 270f * Utils.Deg2Rad;
        camera.fixFisheyeEffect = false;

        window.tick += Tick;

        fpsDisplay = new("?? fps", new(1f, 1f, 200f, 40f), FontFamily.GenericMonospace, 10f);
        renderer.texts.Add(fpsDisplay);
        window.pulse += () => {
            fpsDisplay.text = fpsCounter.ToString("00 fps");
            fpsCounter = 0;
        };

        renderer.map = map;
        map.texturesStr = [null, "wall", "pillar"];
        map.floorTexStr = "floor";
        map.ceilTexStr = "ceiling";
        //renderer.postProcessEffects.Add(new DistanceFog(Renderer.GetDistanceFog, renderer.depthBuf));
        //renderer.postProcessEffects.Add(new VDistortion(x => MathF.Sin(2.5f * (window.timeElapsed + x)) / 20f));
        //renderer.postProcessEffects.Add(new HDistortion(x => MathF.Cos(2.5f * (window.timeElapsed + x)) / 20f));
        //renderer.postProcessEffects.Add(new HVDistortion(x => MathF.Sin(2.5f * (window.timeElapsed + x)) / 20f, x => MathF.Cos(2.5f * (window.timeElapsed + x)) / 20f));

        renderer.sprites.Add(olafScholz = new(camera.pos, new(.8f), true, Resources.sprites["oli"]));
        olafScholzAudio = new(Resources.audios["scholz_speech_1"]) {
            loop = true
        };
        //olafScholzAudio.Play();

        olafPathfinding = new(map, new BreadthFirstSearch());
        window.pulse += () => olafPathfinding.FindPath(olafScholz.pos, mpHandler.GetClientState(mpHandler.serverState.olafTarget).pos);
        //olafPathfinder = new(map, map.size/2f, olafScholz.size.x/2f, olafSpeed);
        //window.pulse += () => olafPathfinder.RefreshPath(mpHandler.GetClientState(mpHandler.serverState.olafTarget)?.pos ?? map.size/2f);

        mpHandler = new(this, host, ip, port, 512, printDebug: false);
        mpHandler.Start();

        mpHandler.serverState.olafPos = map.size/2f;
        mpHandler.serverState.olafTarget = 1;
        mpHandler.SendServerStateChange(StateKey.S_OlafPos, StateKey.S_OlafTarget);

        mpHandler.onFinishConnect += () => {
            mpHandler.ownClientState.skinIdx = skinIdx;
            mpHandler.SendClientStateChange(StateKey.C_Skin);
            mpHandler.SendClientRequest(RequestKey.C_UpdateSkin);

            foreach(var (id, state) in mpHandler.clientStates)
                if(id != mpHandler.ownClientId)
                {
                    Image skin = skins[state.skinIdx];
                    SpriteRenderer newPlayerRenderer = new(state.pos, new((float)skin.Width/skin.Height, 1f), true, skin);
                    renderer.sprites.Add(newPlayerRenderer);
                    playerRenderers.Add((id, newPlayerRenderer));
                }
        };
        mpHandler.onPlayerConnect += id => {
            if(id != mpHandler.ownClientId)
            {
                ClientState state = mpHandler.GetClientState(id);
                Image skin = skins[state.skinIdx];
                SpriteRenderer newPlayerRenderer = new(state.pos, new((float)skin.Width/skin.Height, 1f), true, skin);
                renderer.sprites.Add(newPlayerRenderer);
                playerRenderers.Add((id, newPlayerRenderer));
            }
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
        mpHandler.ownClientState.pos = camera.pos;
        if(mpHandler.isHost)
        {
            mpHandler.serverState.olafPos = olafScholz.pos = camera.pos;
            mpHandler.SendServerStateChange(StateKey.S_OlafPos);
        }
        mpHandler.SendClientStateChange(StateKey.C_Pos);
        camera.maxDist = 30f;
    }

    public void ReloadSkins()
    {
        foreach(var (id, renderer) in playerRenderers)
            if(id != mpHandler.ownClientId)
                renderer.SetImage(skins[mpHandler.GetClientState(id).skinIdx], true);
    }


    private void Tick(float dt)
    {
        fpsCounter++;

        if(!mpHandler.ready)
            return;

        #region Input
        camera.pos = mpHandler.ownClientState.pos;
        Vec2f prevCamPos = camera.pos;
        camera.pos += playerSpeed * dt * (
            (input.KeyHelt(Keys.A) ? 1f : input.KeyHelt(Keys.D) ? -1f : 0f) * camera.right +
            (input.KeyHelt(Keys.S) ? -1f : input.KeyHelt(Keys.W) ? 1f : 0f) * camera.forward).normalized;
        camera.pos = map.ResolveIntersectionIfNecessery(prevCamPos, camera.pos, .25f, out _);
        if(input.lockCursor)
            camera.angle += input.mouseDelta.x * renderer.downscaleFactor * sensitivity / dt;
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

        if(input.KeyDown(Keys.F1))
            input.lockCursor ^= true;

        if(input.KeyDown(Keys.Escape))
            Environment.Exit(0);

        if(input.KeyDown(Keys.F3))
            Debugger.Break();

        if(input.KeyDown(Keys.E))
            mpHandler.SendClientRequest(RequestKey.C_MakeMeOlafTarget);
        #endregion

        Vec2f olafTarget = mpHandler.GetClientState(mpHandler.serverState.olafTarget).pos;
        Vec2f olafToPlayer = olafTarget - olafScholz.pos;
        olafScholz.pos = olafPathfinding.MoveTowards(olafScholz.pos, olafScholz.size.x/2f, olafSpeed, dt);
        //Out(olafScholz.pos);

        //Vec2f oldOlaf = olafScholz.pos;
        //if(olafTarget != olafScholz.pos)
        //    olafScholz.pos += olafToPlayer.normalized * olafSpeed * dt;
        //olafPathfinder.Tick(dt);
        //olafScholz.pos = olafPathfinder.pos;//map.ResolveIntersectionIfNecessery(oldOlaf, olafPathfinder.pos, olafScholz.size.x/2f, out _);
        olafScholzAudio.volume = MathF.Pow(1f - olafToPlayer.length / 10f, 3f);


        if(mpHandler.isHost)
        {
            mpHandler.serverState.olafPos = olafScholz.pos;
            mpHandler.SendServerStateChange(StateKey.S_OlafPos);
        }
        else
        {
            olafScholz.pos = mpHandler.serverState.olafPos;
        }

        foreach(var (id, rend) in playerRenderers)
            rend.pos = mpHandler.GetClientState(id).pos;
    }
}