﻿using System;
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

    private readonly PictureBoxWithDrawOptions pictureBox;
    private readonly DateTime startTime;
    private readonly Thread pulseThread;
    private bool _cursorVisible;


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
            if(!Visible)
                return;

            if(value && !_cursorVisible)
                Cursor.Show();
            else if(!value && _cursorVisible)
                Cursor.Hide();

            _cursorVisible = value;
        }
    }


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
        startTime = DateTime.UtcNow;

        renderer = new(virtualResolution, Size, this);
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
        FormClosed += (_, args) => {
            Application.Exit();
            Environment.Exit((int)args.CloseReason);
        };
        Shown += (_, _) => cursorVisible = false;

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
        Shown += (_, _) => pulseThread.Start();

        // Start processes
        load?.Invoke(this);
        new Thread(() => Application.Run(this)).Start();
        BeginGameLoop();
    }


    public void SetIcon(string manifest)
    {
        if(manifest is not null)
            Icon = Resources.icons[manifest];
    }


    private void BeginGameLoop()
    {
        while(!Visible)
            Thread.Sleep(1);

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