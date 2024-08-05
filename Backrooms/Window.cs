using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using Backrooms.Coroutines;
using System.Collections;
using Backrooms.Debugging;

namespace Backrooms;

public class Window : Form
{
    public Renderer renderer;
    public Input input;
    public DevConsole console;
    public event Action<float> tick, fixedTick;
    public event Action pulse;
    public event Action visible;
    public readonly Screen screen;
    public readonly string[] cmdArgs;
    public float fpsCountFreq = 1f;
    public float fixedDeltaTime = 1/32f;

    private readonly PictureBoxWithDrawOptions pictureBox;
    private readonly DateTime startTime;
    private bool _cursorVisible;
    private int lastFrameCount;
    private float fpsTimer;


    public string title
    {
        get => Text;
        set => Text = value;
    }
    public float timeElapsed { get; private set; }
    public float fixedTimeElapsed { get; private set; }
    public int pulsesElapsed { get; private set; }
    public float deltaTime { get; private set; }
    public bool cursorVisible
    {
        get => _cursorVisible;
        set {
            if(!Visible || (_cursorVisible == value))
                return;

            pictureBox.Cursor = value ? Cursors.Default : new(Resources.GetManifestStream("Resources.Textures.nocursor.cur"));
            _cursorVisible = value;
        }
    }
    public int frameCount { get; private set; }
    public int fixedFrameCount { get; private set; }
    public int currFps { get; private set; }
    public float fixedLoopsPerSecond
    {
        get => 1f / fixedDeltaTime;
        set => fixedDeltaTime = 1f / value;
    }


    public Window(Vec2i virtualResolution, string windowTitle, string[] cmdArgs, string iconManifest, bool lockCursor, bool hideCursor, Action<Window> load = null, Action<float> tick = null)
    {
        DevConsole.Hide();

        this.cmdArgs = cmdArgs;
        CmdArgInterpreter interpretedArgs = new(cmdArgs);
        if(interpretedArgs.invalid) interpretedArgs = null;

        // Initialize
        screen = interpretedArgs?.screen is null ? Screen.FromPoint(Cursor.Position) : Screen.AllScreens[interpretedArgs.screen.Value];
        DoubleBuffered = true;
        BackColor = Color.Black;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        Size = screen.Bounds.Size;
        WindowState = FormWindowState.Maximized;
        Location = screen.WorkingArea.Location;
        SetIcon(iconManifest);
        title = windowTitle;
        startTime = DateTime.UtcNow;

        renderer = new(virtualResolution, (Vec2i)Size, this);
        input = new(renderer, (Vec2i)Location, lockCursor);
        renderer.input = input;
        console = new(this, () => Visible);

        CheckForIllegalCrossThreadCalls = false;
        pictureBox = new() {
            Size = new(renderer.outputRes.x, renderer.outputRes.y),
            Location = new(renderer.outputLocation.x, renderer.outputLocation.y),
            SizeMode = PictureBoxSizeMode.StretchImage,
            InterpolationMode = InterpolationMode.NearestNeighbor,
            SmoothingMode = SmoothingMode.None,
            PixelOffsetMode = PixelOffsetMode.Half,
            CompositingQuality = CompositingQuality.HighSpeed,
            Image = new Bitmap(1, 1)
        };
        Controls.Add(pictureBox);

        renderer.dimensionsChanged += () => {
            input.OnUpdateDimensions(renderer);
            pictureBox.Size = new(renderer.outputRes.x, renderer.outputRes.y);
            pictureBox.Location = new(renderer.outputLocation.x, renderer.outputLocation.y);
        };

        // Add callbacks
        if(tick is not null)
            this.tick += tick;
        this.tick += _ => input.Tick();
        KeyDown += (_, args) => input.CB_OnKeyDown(args.KeyCode);
        KeyUp += (_, args) => input.CB_OnKeyUp(args.KeyCode);
        pictureBox.MouseDown += (_, args) => input.CB_OnKeyDown(args.Button.ToKey());
        pictureBox.MouseUp += (_, args) => input.CB_OnKeyUp(args.Button.ToKey());
        FormClosed += (_, args) => Exit((int)args.CloseReason);

        _cursorVisible = true;
        input.lockCursor = false;
        if(hideCursor)
            Shown += (_, _) => SetCursor(false);

        // Start processes
        load?.Invoke(this);
        new Thread(() => {
            AppDomain.CurrentDomain.UnhandledException += (_, _) => DevConsole.Restore();
            Application.Run(this);
        }).Start();
        BeginGameLoop();
    }


    public void SetIcon(string manifest)
    {
        if(manifest is not null)
            Icon = Resources.icons[manifest];
    }

    public Coroutine StartCoroutine(IEnumerator iterator)
        => new(this, iterator);

    public Delegate[] GetTickInvocationList()
        => tick?.GetInvocationList();


    private void BeginGameLoop()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, _) => DevConsole.Restore();

        while(!Visible)
            Thread.Sleep(1);

        visible?.Invoke();
        DateTime lastFrame = DateTime.UtcNow;
        Bitmap backbuf = new(renderer.virtRes.x, renderer.virtRes.y);
        pictureBox.Image = new Bitmap(renderer.virtRes.x, renderer.virtRes.y);

        while(Visible)
            try
            {
                DateTime now = DateTime.UtcNow;
                timeElapsed = (float)(now - startTime).TotalSeconds;
                deltaTime = (float)(now - lastFrame).TotalSeconds;
                lastFrame = now;

                frameCount++;
                fpsTimer += deltaTime;
                if(fpsTimer >= fpsCountFreq)
                {
                    fpsTimer = 0f;
                    currFps = (int)((frameCount - lastFrameCount) / fpsCountFreq);
                    lastFrameCount = frameCount;
                }

                tick?.Invoke(deltaTime);

                // Invoke fixedTick, while the delta between real time and fixed time > fixed delta time
                while(timeElapsed - fixedTimeElapsed > fixedDeltaTime)
                {
                    fixedTick?.Invoke(fixedDeltaTime);
                    fixedTimeElapsed += fixedDeltaTime;
                }

                // Invoke pulse, while delta between real time and fixed time > 1
                while(timeElapsed - pulsesElapsed > 1f)
                {
                    pulse?.Invoke();
                    pulsesElapsed++;
                }

                if(renderer.PrepareDraw())
                    renderer.Draw(backbuf);

                (pictureBox.Image, backbuf) = (backbuf, pictureBox.Image as Bitmap);
            }
            catch(InvalidOperationException exc)
            {
                Out(Log.Info, $"InvlidOperationException in main UpdateLoop (Window.cs) ;; {exc.Message}");
            }
            catch(Exception exc)
            {
                OutErr(Log.Info, exc, $"{exc.GetType()} in main UpdateLoop (Window.cs) ;; $e");
            }
    }

    public void SetCursor(bool freeAndVisible)
    {
        cursorVisible = freeAndVisible;
        input.lockCursor = !freeAndVisible;
    }

    public void ToggleCursor()
        => SetCursor(!cursorVisible);


    public static void Exit(int exitCode = 0)
    {
        DevConsole.Restore();
        Application.Exit();
        Environment.Exit(exitCode);
    }
}