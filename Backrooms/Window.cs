using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;

namespace Backrooms;

public class Window : Form
{
    public Renderer renderer;
    public Input input;
    public DevConsole console;
    public event Action<float> tick;
    public event Action pulse;
    public event Action onWindowVisible;

    private readonly PictureBoxWithDrawOptions pictureBox;
    private readonly Stopwatch timeElapsedSw;
    private readonly Thread pulseThread;


    public string title
    {
        get => Text;
        set => Text = value;
    }
    public float timeElapsed => (float)timeElapsedSw.Elapsed.TotalSeconds;
    public float deltaTime { get; private set; }


    public Window(Vec2i virtualResolution, string windowTitle, string iconManifest, bool lockCursor, Action<Window> load = null, Action<float> tick = null)
    {
        DevConsole.Hide();

        // Initialize
        DoubleBuffered = true;
        BackColor = Color.Black;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        Size = Screen.PrimaryScreen.Bounds.Size;
        WindowState = FormWindowState.Maximized;
        Location = Screen.FromPoint(Cursor.Position).WorkingArea.Location;
        SetIcon(iconManifest);
        title = windowTitle;

        renderer = new(virtualResolution, Size, this);
        input = new(renderer.physRes, (Vec2i)Location, lockCursor);
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
        FormClosed += (_, _) => Environment.Exit(0);
        Shown += (_, _) => Cursor.Hide();

        // Start pulse timer
        pulseThread = new(() => {
            async void invoke()
                => await Task.Run(pulse);

            while(Visible)
            {
                Thread.Sleep(1000);
                invoke();
            }
        }) {
            IsBackground = true
        };
        onWindowVisible += pulseThread.Start;

        // Start processes
        load?.Invoke(this);
        (timeElapsedSw = new()).Start();
        new Thread(() => Application.Run(this)).Start();
        BeginGameLoop();
    }


    public void SetIcon(string manifest)
    {
        if(manifest is not null)
            Icon = Resources.icons[manifest];
    }

    public void BeginGameLoop()
    {
        while(!Visible)
            Thread.Sleep(1);

        onWindowVisible();

        DateTime lastFrame = DateTime.UtcNow;
        while(Visible)
            try
            {
                DateTime now = DateTime.UtcNow;
                deltaTime = (float)(now - lastFrame).TotalSeconds;
                lastFrame = now;

                tick?.Invoke(deltaTime);

                Bitmap renderResult = renderer.Draw(); 

                Image lastImg = pictureBox.Image;
                pictureBox.Image = renderResult;
                lastImg.Dispose();
            }
            catch(Exception exc)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{exc.GetType()} in Draw(), Window.cs:\n{exc}");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
    }
}