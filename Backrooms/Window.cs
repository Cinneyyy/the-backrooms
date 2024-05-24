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
    public Thread renderThread;
    public Renderer renderer;
    public Input input;
    public event Action<float> tick;
    public event Action pulse;

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
        pictureBox = new() {
            Size = new(renderer.outputRes.x, renderer.outputRes.y),
            Location = new(renderer.outputLocation.x, renderer.outputLocation.y),
            SizeMode = PictureBoxSizeMode.StretchImage,
            InterpolationMode = InterpolationMode.NearestNeighbor,
            SmoothingMode = SmoothingMode.None,
            PixelOffsetMode = PixelOffsetMode.Half,
            CompositingQuality = CompositingQuality.HighSpeed
        };
        Controls.Add(pictureBox);

        renderer.dimensionsChanged += () => {
            pictureBox.Size = new(renderer.outputRes.x, renderer.outputRes.y);
            pictureBox.Location = new(renderer.outputLocation.x, renderer.outputLocation.y);
            input.OnUpdateDimensions(renderer);
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
            while(!Visible)
                Thread.Sleep(1);

            while(Visible)
            {
                Thread.Sleep(1000);

                async void invoke()
                    => await Task.Run(pulse);

                invoke();
            }
        }) {
            IsBackground = true
        };
        pulseThread.Start();

        // Start processes
        load?.Invoke(this);
        (timeElapsedSw = new()).Start();
        (renderThread = new(Draw)).Start();
        Application.Run(this);
    }


    public void SetIcon(string manifest)
    {
        if(manifest is not null)
            Icon = Resources.icons[manifest];
    }

    public void Draw()
    {
        while(!Visible)
            Thread.Sleep(1);

        DateTime lastFrame = DateTime.UtcNow;
        while(Visible)
        {
            try
            {
                DateTime now = DateTime.UtcNow;
                deltaTime = (float)(now - lastFrame).TotalSeconds;
                lastFrame = now;

                tick?.Invoke(deltaTime);

                Bitmap renderResult = renderer.Draw();
                pictureBox.Image = renderResult;
            }
            catch(Exception exc)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{exc.GetType()} in Draw(), Window.cs:\n{exc}");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}