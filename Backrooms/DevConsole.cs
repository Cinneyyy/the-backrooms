using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Backrooms.Gui;

namespace Backrooms;

public partial class DevConsole : IEnumerable<DevConsole.Cmd>
{
    public readonly record struct Cmd(string[] identifiers, Action<string[]> invoke, string syntax, int[] argCounts);

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

                // Execute next tick, as to avoid race conditions
                nextTick = () => {
                    Cmd cmd;

                    try
                    {
                        cmd = cmds.Where(cmd => cmd.identifiers.Contains(args[0].ToLower())).First();

                        if(!cmd.argCounts.Contains(args.Length-1))
                            throw new ArgumentException($"The {args[0].ToUpper()} command does not take in {args.Length-1} argument{(args.Length-1 != 1 ? "s" : "")}");
                    }
                    catch(ArgumentException)
                    {
                        throw;
                    }
                    catch
                    {
                        throw new($"The command '{args[0]}' does not seem to exist");
                    }

                    cmd.invoke(args[1..]);
                };
            }
        }) {
            IsBackground = true
        };

        win.Shown += (_, _) => thread.Start();

        cmds = [
            new(["help", "?", "cmd_list", "cmds", "commands", "command_list"], args => {
                if(args.Length == 1)
                {
                    Cmd cmd = cmds.Where(c => c.identifiers.Contains(args[0].ToLower())).First();
                    Out($"{cmd.syntax}\n    => Aliases: {cmd.identifiers.FormatStr(", ", i => i.ToUpper())}");
                }
                else if(args.Length == 0)
                {
                    Out("-- List of commands --");
                    foreach(Cmd cmd in cmds)
                        Out($"{cmd.syntax}\n    => Aliases: {cmd.identifiers.FormatStr(", ", i => i.ToUpper())}");
                }
            }, 
            "HELP [<command>]", [0, 1]),

            new(["aliases", "alias"], args => Out($"Aliases of the {args[0].ToUpper()} command: {cmds.Where(c => c.identifiers.Contains(args[0].ToLower())).First().identifiers.FormatStr(", ", id => id.ToUpper())}"),
            "ALIASES <cmd>", [1]),

            new(["resolution", "res", "set_resolution", "set_res"], args => {
                if(args[0] is "query" or "q" or "?")
                {
                    Out($"The current resolution is {win.renderer.virtRes.ToString("{0}x{1}")}");
                    return;
                }
                
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
                        float fac = float.Parse((args[1]));
                        Vec2i physRes = win.renderer.physRes;
                        Vec2i virtRes = (args[0] == "/" ? (physRes / fac) : (physRes * fac)).Round();
                        win.renderer.UpdateResolution(virtRes, physRes);
                        Out($"Set virtual resolution to {virtRes}");
                        return;
                    }

                    xStr = args[0];
                    yStr = args[1];
                }
                else
                    return;

                Vec2i res = Vec2i.Parse(xStr, yStr);
                win.renderer.UpdateResolution(res, win.renderer.physRes);
                Out($"Set virtual resolution to {res}");
            }, 
            "SET_RESOLUTION [<width[x|:|/]height> | <width height> | <[*|/] factor>]", [1, 2]),

            new(["fov", "set_fov", "field_of_view", "set_field_of_view"], args => {
                args[0] = args[0].ToLower();

                if(args[0] is "query" or "q" or "?")
                {
                    Out($"The current fov value is {win.renderer.camera.fov} ({win.renderer.camera.fov/MathF.PI :0.00}pi ;; {win.renderer.camera.fov*Utils.Rad2Deg :0.00}°)");
                    return;
                }

                string valueStr;
                int unit = 0; // radians, degrees, raw value
                if(args[0][^1] == '°')
                {
                    valueStr = args[0][..^1];
                    unit = 1;
                }
                else if(args[0].EndsWith("pi"))
                {
                    valueStr = args[0][..^2];
                    unit = 0;
                }
                else if(args[0].EndsWith("deg") || args[0].EndsWith("rad"))
                {
                    valueStr = args[0][..^3];
                    unit = args[0][^1] == 'g' ? 1 : 0;
                }
                else
                {
                    valueStr = args[0];
                    unit = 2;
                }

                float rawValue = float.Parse(valueStr);
                if(unit == 0) rawValue *= MathF.PI;
                else if(unit == 1) rawValue *= Utils.Deg2Rad;

                Camera cam = win.renderer.camera;
                cam.fov = rawValue;
                Out($"Set FOV to {cam.fov:0.00} ({cam.fov/MathF.PI:0.00}pi ;; {cam.fov*Utils.Rad2Deg:0.00}°)");
            },
            "FOV <value[°|pi|deg|rad|]>", [1]),

            new(["hide", "close", "hide_console", "close_console"], args => Hide(), 
            "HIDE", [0]),

            new(["fps_display", "fps", "show_fps"], args => ParseBool(args.ElementAtOrDefault(0) ?? "^", win.renderer.FindGuiGroup("hud").FindElement("fps"), e => e.enabled), 
            "SHOW_FPS <enabled>", [0, 1]),

            new(["parallel_render", "para_render", "use_parallel_render", "use_para_render"], args => ParseBool(args.ElementAtOrDefault(0) ?? "^", ref win.renderer.useParallelRendering), 
            "PARALLEL_RENDER <enabled>", [0, 1]),

            new(["fisheye_fix", "ff", "fix_fisheye_effect"], args => ParseBool(args.ElementAtOrDefault(0) ?? "^", ref win.renderer.camera.fixFisheyeEffect), 
            "FISHEYE_FIX <enabled>", [0, 1]),

            new(["cursor", "cursor_visible", "show_cursor"], args => ParseBool(args.ElementAtOrDefault(0) ?? "^", win, w => w.cursorVisible),
            "CURSOR_VISIBLE <enabled>", [0, 1])
        ];
    }


    public void Add(Cmd cmd)
    {
        Array.Resize(ref cmds, cmds.Length+1);
        cmds[^1] = cmd;
    }

    public IEnumerator<Cmd> GetEnumerator()
    {
        foreach(Cmd cmd in cmds)
            yield return cmd;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    private void Tick(float dt)
    {
        if(nextTick is not null)
            try
            {
                nextTick();
            }
            catch(Exception exc)
            {
                Out($"{exc.GetType().Name}: {exc.Message}", ConsoleColor.Red);
            }
            finally
            {
                nextTick = null;
                Console.ForegroundColor = ConsoleColor.Gray;
            }
    }


    public static void ParseBool(string strVal, ref bool target, bool throwExcIfFailed = true)
    {
        if(string.IsNullOrWhiteSpace(strVal))
            if(throwExcIfFailed) 
                throw new ArgumentException("Invalid input for ParseBool (null/whitspace)");
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
            case "q" or "query" or "?":
                Out($"The current value is {target}");
                break;
            default:
                if(throwExcIfFailed)
                    throw new($"Invalid input for ParseBool: \"{strVal}\"");
                else
                    break;
        }
    }
    public static void ParseBool(string strVal, Func<bool> get, Action<bool> set, bool throwExcIfFailed = true)
    {
        bool value = get();
        ParseBool(strVal, ref value, throwExcIfFailed);
        set(value);
    }
    public static void ParseBool<T>(string strVal, T target, Expression<Func<T, bool>> outExpr, bool throwExcIfFailed = true)
    {
        MemberExpression expr = outExpr.Body as MemberExpression;
        PropertyInfo prop = expr.Member as PropertyInfo;
        bool value = (bool)prop.GetValue(target);
        ParseBool(strVal, ref value, throwExcIfFailed);
        prop.SetValue(target, value);
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