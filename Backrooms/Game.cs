using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
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
    public MPHandler mpHandler;
    public readonly List<(byte id, SpriteRenderer renderer)> playerRenderers = [];


    private readonly RoomGenerator generator = new();
    //private readonly Timer fpsTimer = new();


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
        //olafScholzAudio.Play();

        mpHandler = new(this, host, "127.0.0.1", 8080, 2048, printDebug: false);
        mpHandler.Start();

        mpHandler.serverState.olafPos = map.size/2f;
        mpHandler.serverState.olafTarget = 1;
        mpHandler.SendServerStateChange(StateKey.S_OlafPos, StateKey.S_OlafTarget);

        mpHandler.onFinishConnect += () => {
            foreach(var (id, state) in mpHandler.clientStates)
                if(id != mpHandler.ownClientId)
                {
                    SpriteRenderer newPlayerRenderer = new(state.pos, new(.25f, .6f), false, Resources.sprites["square"]);
                    renderer.sprites.Add(newPlayerRenderer);
                    playerRenderers.Add((id, newPlayerRenderer));
                }
        };
        mpHandler.onPlayerConnect += id => {
            if(id != mpHandler.ownClientId)
            {
                SpriteRenderer newPlayerRenderer = new(mpHandler.GetClientState(id).pos, new(.25f, .6f), false, Resources.sprites["square"]);
                renderer.sprites.Add(newPlayerRenderer);
                playerRenderers.Add((id, newPlayerRenderer));
            }
        };
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
            camera.angle += input.mouseDelta.x * renderer.downscaleFactor * sensitivity * dt;
        mpHandler.ownClientState.pos = camera.pos;
        mpHandler.ownClientState.rot = camera.angle;
        mpHandler.SendClientStateChange(StateKey.C_Pos);

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

        Vec2f olafTarget = mpHandler.GetClientState(mpHandler.serverState.olafTarget).pos;
        Vec2f olafToPlayer = olafTarget - olafScholz.pos;
        Vec2f oldOlaf = olafScholz.pos;
        if(olafTarget != olafScholz.pos)
            olafScholz.pos += olafToPlayer.normalized * olafSpeed * dt;
        olafScholz.pos = map.ResolveIntersectionIfNecessery(oldOlaf, olafScholz.pos, olafScholz.size.x/2f, out _);
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