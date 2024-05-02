using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace Backrooms;

public class Window : Form
{
    public Thread renderThread;
    public Renderer renderer;
    public Input input;
    public event Action<float> tick;

    private readonly PictureBoxWithDrawOptions pictureBox;
    private readonly Stopwatch timeElapsedSw;


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
        input = new(renderer.physicalRes, (Vec2i)Location, lockCursor);
        renderer.input = input;
        pictureBox = new() {
            Size = new(renderer.outputRes.x, renderer.outputRes.y),
            Location = new(renderer.outputLocation.x, renderer.outputLocation.y),
            SizeMode = PictureBoxSizeMode.StretchImage,
            InterpolationMode = InterpolationMode.NearestNeighbor,
            SmoothingMode = SmoothingMode.None,
            PixelOffsetMode = PixelOffsetMode.Half,
            CompositingQuality = CompositingQuality.HighQuality
        };
        Controls.Add(pictureBox);

        // Add callbacks
        this.tick += tick;
        this.tick += _ => input.Tick();
        KeyDown += (_, args) => input.CB_OnKeyDown(args.KeyCode);
        KeyUp += (_, args) => input.CB_OnKeyUp(args.KeyCode);

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
        AppDomain.CurrentDomain.UnhandledException += (_, exc) => {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Unhandled exception in RenderThread:\n{exc}");
            Console.ForegroundColor = ConsoleColor.Gray;
            renderThread.Start();
        };

        try
        {
            while(!Visible)
                Thread.Sleep(10);

            Stopwatch sw = new();
            while(Visible)
            {
                tick?.Invoke(deltaTime = (float)sw.Elapsed.TotalSeconds);
                sw.Restart();

                Bitmap renderResult = renderer.Draw();
                Thread.Sleep(1);
                pictureBox.Image = renderResult;
            }
        }
        catch(InvalidOperationException exc)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"InvalidOperationException in Draw(), Window.cs:\n{exc}");
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}