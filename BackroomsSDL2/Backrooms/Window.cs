using System;

namespace Backrooms;

#pragma warning disable CA1806 // Do not ignore method results
public static class Window
{
    public delegate void Tick(float dt);


    private static bool isRunning;

    public static event Tick tickDt;
    public static event Action tick;


    public static nint sdlWind { get; private set; }
    public static float totalTime { get; private set; }
    public static float deltaTime { get; private set; }
    public static int fps { get; private set; }

    private static Vec2i _size;
    public static Vec2i size
    {
        get => _size;
        set
        {
            _size = value;
            SDL_SetWindowSize(sdlWind, value.x, value.y);
        }
    }

    private static string _title;
    public static string title
    {
        get => _title;
        set
        {
            _title = value;
            SDL_SetWindowTitle(sdlWind, value);
        }
    }


    public static void Init(Vec2i size, string title)
    {
        if(isRunning)
            throw new("Cannot initialize window while it's already running");

        SDL_SetHint(SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");
        SDL_SetHint(SDL_HINT_APP_NAME, title);

        if(SDL_Init(SDL_INIT_VIDEO) < 0)
            throw new($"Failed to initialize SDL: {SDL_GetError()}");

        if(TTF_Init() < 0)
            throw new($"Failed to initialize TTF: {TTF_GetError()}");

        if(IMG_Init(IMG_InitFlags.IMG_INIT_JPG | IMG_InitFlags.IMG_INIT_PNG) < 0)
            throw new($"Failed to initialize IMG: {IMG_GetError()}");

        _size = size;
        _title = title;

        SDL_GetDisplayBounds(0, out SDL_Rect screen);
        sdlWind = SDL_CreateWindow(title, SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED, screen.w, screen.h, SDL_WindowFlags.SDL_WINDOW_BORDERLESS);
        if(sdlWind == nint.Zero)
            throw new($"Failed to create SDL window: {SDL_GetError()}");

        Renderer.Init(sdlWind, size, new(screen.w, screen.h));

        CameraMovement.Init();

        Input.relativeMouse = true;
    }

    public static void Run()
    {
        isRunning = true;

        DateTime lastFrame = DateTime.UtcNow;
        DateTime firstFrame = DateTime.UtcNow;

        float fpsTimer = 0f;
        int frameCount = 0;
        const float fps_time_frame = 1f;

        while(isRunning)
        {
            HandleEvents();

            if(Input.KeyDown(Key.F1))
                Input.relativeMouse ^= true;
            if(Input.KeyDown(Key.F2))
                CameraMovement.noClip ^= true;

            DateTime now = DateTime.UtcNow;
            deltaTime = (float)(now - lastFrame).TotalSeconds;
            totalTime = (float)(now - firstFrame).TotalSeconds;
            lastFrame = now;

            fpsTimer += deltaTime;
            frameCount++;
            if(fpsTimer >= fps_time_frame)
            {
                fps = (int)(frameCount / fps_time_frame);
                frameCount = 0;
                fpsTimer = 0f;

                Console.WriteLine(fps);
            }

            tick?.Invoke();
            tickDt?.Invoke(deltaTime);

            Renderer.Draw();
        }

        Renderer.DestroyTex();
        SDL_DestroyRenderer(Renderer.sdlRend);
        SDL_DestroyWindow(sdlWind);
        SDL_Quit();
    }


    private static void HandleEvents()
    {
        static int mouseButtonToKeyCode(byte mb)
            => mb switch
            {
                0 => (int)Key.Lmb,
                1 => (int)Key.Mmb,
                2 => (int)Key.Rmb,
                _ => 0
            };

        Input.Internal.TickPrePolling();

        while(SDL_PollEvent(out SDL_Event evt) == 1)
            switch(evt.type)
            {
                case SDL_EventType.SDL_QUIT:
                {
                    isRunning = false;
                    break;
                }
                case SDL_EventType.SDL_KEYDOWN:
                {
                    if(evt.key.keysym.sym == SDL_Keycode.SDLK_ESCAPE)
                        goto case SDL_EventType.SDL_QUIT;

                    Input.Internal.KeyDown((int)evt.key.keysym.sym);
                    break;
                }
                case SDL_EventType.SDL_KEYUP:
                {
                    Input.Internal.KeyUp((int)evt.key.keysym.sym);
                    break;
                }
                case SDL_EventType.SDL_MOUSEBUTTONDOWN:
                {
                    Input.Internal.KeyDown(mouseButtonToKeyCode(evt.button.button));
                    break;
                }
                case SDL_EventType.SDL_MOUSEBUTTONUP:
                {
                    Input.Internal.KeyUp(mouseButtonToKeyCode(evt.button.button));
                    break;
                }
                case SDL_EventType.SDL_MOUSEMOTION:
                {
                    SDL_MouseMotionEvent m = evt.motion;
                    Input.Internal.MouseMove(new(m.x, m.y), new(m.xrel, m.yrel));
                    break;
                }
            }

        Input.Internal.TickPostPolling();
    }
}