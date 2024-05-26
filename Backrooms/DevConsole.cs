using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Backrooms;

public partial class DevConsole
{
    public readonly record struct Cmd(string[] identifiers, Action<string[]> invoke, string syntax);

    public enum WindowMode
    {
        Hide = 0,
        Maximize = 3,
        Minimize = 6,
        Restore = 9
    }


    public Window win;
    public Cmd[] cmds;
    public Func<bool> run;

    private readonly Thread thread;
    private Action nextTick;

    private static readonly nint consoleHandle = GetConsoleWindow();
    private static WindowMode _windowMode = WindowMode.Restore;


    public static WindowMode windowMode
    {
        get => _windowMode;
        set => ShowWindow(_windowMode = value);
    }


    public DevConsole(Window win, Func<bool> run)
    {
        this.win = win;
        win.tick += Tick;

        thread = new(() => {
            while(run())
            {
                string input = Console.ReadLine();

                if(string.IsNullOrWhiteSpace(input))
                    continue;

                string[] args = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                try
                {
                    nextTick = () => (cmds.Where(cmd => cmd.identifiers.Contains(args[0]))?.First() ?? throw new($"The command '{args[0]}' does not seem to exist")).invoke(args[1..]);
                }
                catch(Exception exc)
                {
                    Out(exc, ConsoleColor.Red);
                }
            }
        });

        win.onWindowVisible += thread.Start;

        cmds = [
            new(["resolution", "res", "set_resolution", "set_res"], args => {
                string xStr, yStr;
                
                if(args.Length == 1)
                {
                    string[] split = args[0].Split(':', 'x', '/');
                    xStr = split[0];
                    yStr = split[1];
                }
                else if(args.Length == 2)
                {
                    if(args[0] is "/" or "*")
                    {
                        int fac = int.Parse((args[1]));
                        Vec2i physRes = win.renderer.physRes;
                        Vec2i virtRes = args[0] == "/" ? (physRes / fac) : (physRes * fac);
                        win.renderer.UpdateResolution(virtRes, physRes);
                        return;
                    }

                    xStr = args[0];
                    yStr = args[1];
                }
                else
                    throw new("The set_resolution command takes in either 1 or 2 overloads!");

                win.renderer.UpdateResolution(Vec2i.Parse(xStr, yStr), win.renderer.physRes);
            }, "SET_RESOLUTION <width[x:/]height> // <width height> // <[*/] factor>")
        ];
    }


    public void Add(Cmd cmd)
    {
        Array.Resize(ref cmds, cmds.Length+1);
        cmds[^1] = cmd;
    }


    private void Tick(float dt)
    {
        if(nextTick is not null)
            try
            {
                nextTick();
                nextTick = null;
            }
            catch(Exception exc)
            {
                Out(exc.Message, ConsoleColor.Red);
            }
    }


    /// <summary>Returns: successful?, target: actual result</summary>
    public static void ParseBool(string strVal, ref bool target, bool throwExcIfFailed)
    {
        if(string.IsNullOrWhiteSpace(strVal))
            if(throwExcIfFailed) 
                throw new("Invalid input for ParseBool (null/whitspace)");
            else 
                return;

        switch(strVal.ToLower())
        {
            case "true" or "t" or "1" or "1b" or "yes" or "y":
                target = true;
                break;
            case "false" or "f" or "0" or "0b" or "no" or "n": 
                target = false; 
                break;
            case "switch" or "s" or "~" or "!" or "^": 
                target ^= true; 
                break;
            default:
                if(throwExcIfFailed)
                    throw new($"Invalid input for ParseBool: \"{strVal}\"");
                else
                    break;
        }
    }

    public static nint PostMessage(ConsoleKey key, uint msg = 0x100u) 
        => PostMessage(consoleHandle, msg, (nint)key, nint.Zero);

    public static bool ShowWindow(WindowMode windowMode)
        => ShowWindow(consoleHandle, (int)(_windowMode = windowMode));
    public static bool ShowWindow(int cmdShow)
    {
        _windowMode = (WindowMode)cmdShow;
        return ShowWindow(consoleHandle, cmdShow);
    }

    public static bool Hide() => ShowWindow(WindowMode.Hide);
    public static bool Maximize() => ShowWindow(WindowMode.Maximize);
    public static bool Minimize() => ShowWindow(WindowMode.Minimize);
    public static bool Restore() => ShowWindow(WindowMode.Restore);


    #region P/Invokes
    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.SysInt)]
    private static partial nint GetConsoleWindow();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(nint hWnd, int cmdShow);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.SysInt)]
    private static partial nint PostMessage(nint hWnd, uint Msg, nint wParam, nint lParam);
    #endregion
}