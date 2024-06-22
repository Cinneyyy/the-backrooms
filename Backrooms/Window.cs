using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using Backrooms.Coroutines;
using System.Collections;

namespace Backrooms;

public class Window : Form
{
    public Renderer renderer;
    public Input input;
    public DevConsole console;
    public event Action<float> tick;
    public event Action pulse;
    public event Action visible;
    public readonly Screen screen;
    public float fpsCountFreq = 1f;

    private readonly PictureBoxWithDrawOptions pictureBox;
    private readonly DateTime startTime;
    private readonly Thread pulseThread;
    private bool _cursorVisible;
    private int lastFrameCount;
    private float fpsTimer;


    public string title
    {
        get => Text;
        set => Text = value;
    }
    public float timeElapsed => (float)(DateTime.UtcNow - startTime).TotalSeconds;
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
    public int currFps { get; private set; }


    public Window(Vec2i virtualResolution, string windowTitle, string iconManifest, bool lockCursor, bool hideCursor, Action<Window> load = null, Action<float> tick = null)
    {
        DevConsole.Hide();

        // Initialize
        screen = Screen.FromPoint(Cursor.Position);
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
        pictureBox.MouseDown += (_, args) => input.CB_OnCursorDown(args.Button);
        pictureBox.MouseUp += (_, args) => input.CB_OnCursorUp(args.Button);
        FormClosed += (_, args) => Exit((int)args.CloseReason);

        _cursorVisible = true;
        if(hideCursor)
            Shown += (_, _) => cursorVisible = false;

        // Start pulse timer
        pulseThread = new(() => {
            AppDomain.CurrentDomain.UnhandledException += (_, _) => DevConsole.Restore();

            async void invoke()
                => await Task.Run(pulse);

            while(Visible)
            {
                Thread.Sleep(1000);
                
                if(pulse is not null)
                    invoke();
            }
        }) {
            IsBackground = true
        };
        Shown += (_, _) => pulseThread.Start();

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


    private void BeginGameLoop()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, _) => DevConsole.Restore();

        while(!Visible)
            Thread.Sleep(1);

        visible?.Invoke();
        DateTime lastFrame = DateTime.UtcNow;
        while(Visible)
            try
            {
                DateTime now = DateTime.UtcNow;
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

                Bitmap renderResult = renderer.Draw(); 

                Image lastImg = pictureBox.Image;
                pictureBox.Image = renderResult;
                lastImg.Dispose();
            }
            catch(Exception exc)
            {
                Out($"{exc.GetType()} in main UpdateLoop (Window.cs) ;; {exc.Message}", ConsoleColor.Red);
            }
    }


    public static void Exit(int exitCode = 0)
    {
        DevConsole.Restore();
        Application.Exit();
        Environment.Exit(exitCode);
    }
}