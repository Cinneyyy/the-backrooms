using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Backrooms.Gui;

namespace Backrooms.Debugging;

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


    public readonly Window win;
    public Cmd[] cmds;
    public Func<bool> run;
    public bool queryIfEmpty = false;

    private readonly Thread thread;
    private Action nextTick;

    private static readonly nint consoleHandle = GetConsoleWindow();
    private static readonly nint stdHandle = GetStdHandle(-11);
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

        GetConsoleMode(stdHandle, out int consoleMode);
        SetConsoleMode(stdHandle, consoleMode | 0b100);

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
            IsBackground = false
        };

        win.Shown += (_, _) => thread.Start();

        cmds = [

            new(["help", "?", "cmd_list", "cmds", "commands", "command_list"],
            args => {
                if(args.Length == 1)
                {
                    Cmd cmd = cmds.Where(c => c.identifiers.Contains(args[0].ToLower())).First();
                    Out(Log.DevCmd, $"{cmd.syntax}\n    => Aliases: {cmd.identifiers.FormatStr(", ", id => id.ToUpper())}");
                }
                else if(args.Length == 0)
                {
                    StringBuilder sb = new("-- List of commands --");

                    foreach(Cmd cmd in cmds)
                        sb.Append($"{cmd.syntax}\n    => Aliases: {cmd.identifiers.FormatStr(", ", id => id.ToUpper())}");

                    Out(Log.DevCmd, sb.ToString());
                }
            },
            "HELP [<command>]", [0, 1]),

            new(["aliases", "alias"],
            args => Out(Log.DevCmd, $"Aliases of the {args[0].ToUpper()} command: {cmds.Where(c => c.identifiers.Contains(args[0].ToLower())).First().identifiers.FormatStr(", ", id => id.ToUpper())}"),
            "ALIASES <cmd>", [1]),

            new(["query_if_empty", "emptyquery"],
            args => ParseBool(args.FirstOrDefault(), ref queryIfEmpty),
            "QUERY_IF_EMPTY <value>", [0, 1]),

            new(["log", "set_log"],
            args =>
            {
                if(args[0] == "?")
                {
                    foreach(Logger logger in loggers)
                        if(logger != GetLogger(Log.DevCmd))
                            WriteLine($"§f0Log type \"§f1{logger.name}§f0\" is currently {(logger.enabled ? "enabled" : "disabled")}", [GetLogger(Log.DevCmd).color, logger.color], [], autoPrefixFB: false);

                    return;
                }

                if(args[0] == "*")
                    args[0] = "*\\DevCmd";

                if(args[0].StartsWith("*\\"))
                    args[0] = Enum.GetNames<Log>()
                        .Where(n => !args[0].Contains(n, StringComparison.OrdinalIgnoreCase))
                        .Where(n => !n.Equals(nameof(Log.DevCmd), StringComparison.OrdinalIgnoreCase))
                        .FormatStr(",");

                foreach(string logName in args[0].Split(','))
                {
                    Log log = Enum.Parse<Log>(logName, true);
                    if(log == Log.DevCmd)
                    {
                        Out(Log.DevCmd, "Cannot disable log type \"DevCmd\"", "");
                        continue;
                    }

                    Logger logger = GetLogger(log);
                    bool prevValue = logger.enabled;
                    ParseBool(args.ElementAtOrDefault(1), ref logger.enabled, logChange: false);
                    WriteLine($"§f0Log type \"§f1{log}§f0\" is {(prevValue == logger.enabled ? "currently" : "now")} {(logger.enabled ? "enabled" : "disabled")}", [GetLogger(Log.DevCmd).color, logger.color], [], autoPrefixFB: false);
                }
            }, "SET_LOG <name|*|*\\exclude|?> <enabled>", [1, 2]),

            new(["sprites_through_walls", "stw", "xray", "sprite_overdraw"],
            args => ParseBool(args.FirstOrDefault(), ref win.renderer.overdrawSprites),
            "SPRITE_OVERDRAW <value>", [0, 1]),

            new(["fog_max_dist", "fog_dist", "fog_max"],
            args => ParseNumber(args.FirstOrDefault(), win.renderer, r => r.fogMaxDist),
            "FOG_MAX_DIST <value>", [0, 1]),

            new(["fog_coefficient", "fog_coeff"],
            args => ParseNumber(args.FirstOrDefault(), win.renderer, r => r.fogCoefficient),
            "FOG_COEFFICIENT <value>", [0, 1]),

            new(["fog_epsilon", "fog_eps"],
            args => ParseNumber(args.FirstOrDefault(), win.renderer, r => r.fogEpsilon),
            "FOG_EPSILON <value>", [0, 1]),

            new(["no_fog", "hide_fog", "unhüll_everything_from_the_thick_nebel"],
            args => ParseBool(args.FirstOrDefault(), () => !win.renderer.fogEnabled, f => win.renderer.fogEnabled = !f),
            "NO_FOG <value>", [0, 1]),

            new(["resolution", "res", "set_resolution", "set_res"],
            args => {
                if(args[0] is "query" or "q" or "?")
                {
                    Out(Log.DevCmd, $"The current resolution is {win.renderer.virtRes.ToString("{0}x{1}")}");
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
                        Out(Log.DevCmd, $"Set virtual resolution to {virtRes}");
                        return;
                    }

                    xStr = args[0];
                    yStr = args[1];
                }
                else
                    return;

                Vec2i res = Vec2i.Parse(xStr, yStr);
                win.renderer.UpdateResolution(res, win.renderer.physRes);
                Out(Log.DevCmd, $"Set virtual resolution to {res}");
            },
            "SET_RESOLUTION [<width[x|:|/]height> | <width height> | <[*|/] factor>]", [1, 2]),

            new(["fov", "set_fov", "field_of_view", "set_field_of_view"],
            args => {
                args[0] = args[0].ToLower();

                if(args[0] is "query" or "q" or "?")
                {
                    Out(Log.DevCmd, $"The current fov value is {win.renderer.camera.fov} ({win.renderer.camera.fov/MathF.PI :0.00}pi ;; {win.renderer.camera.fov*Utils.Rad2Deg :0.00}°)");
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
                Out(Log.DevCmd, $"Set FOV to {cam.fov:0.00} ({cam.fov/MathF.PI:0.00}pi ;; {cam.fov*Utils.Rad2Deg:0.00}°)");
            },
            "FOV <value[°|pi|deg|rad|]>", [1]),

            new(["hide", "close", "hide_console", "close_console"],
            args => Hide(),
            "HIDE", [0]),

            new(["fps_display", "fps", "show_fps"],
            args => ParseBool(args.FirstOrDefault(), win.renderer.FindGuiGroup("hud").GetElement("fps"), e => e.enabled),
            "SHOW_FPS <enabled>", [0, 1]),

            new(["cursor", "cursor_visible", "show_cursor"],
            args => ParseBool(args.FirstOrDefault(), () => win.cursorVisible, b => win.SetCursor(b)),
            "CURSOR_VISIBLE <enabled>", [0, 1]),

            new(["wall_height", "wheight", "wallh"],
            args => ParseNumber(args.FirstOrDefault(), ref win.renderer.wallHeight),
            "WALL_HEIGHT <height>", [0, 1]),

            new(["gui_elem_size", "elem_size"],
            args => {
                GuiElement elem = win.renderer.FindGuiGroup(args[0]).GetElement(args[1]);
                Vec2f size = elem.size;
                ParseVector(args.ElementAtOrDefault(2), ref size);
                elem.size = size;
            },
            "ELEM_SIZE <group> <element>", [2, 3]),

            new(["gui_elem_loc", "elem_loc"],
            args => {
                GuiElement elem = win.renderer.FindGuiGroup(args[0]).GetElement(args[1]);
                Vec2f loc = elem.location;
                ParseVector(args.ElementAtOrDefault(2), ref loc);
                elem.size = loc;
            },
            "ELEM_LOC <group> <element>", [2, 3]),

            new(["list_gui_groups", "gui_groups"],
            args => Out(Log.DevCmd, win.renderer.guiGroups.FormatStr(", ", g => g.name)),
            "LIST_GUI_GROUPS", [0]),

            new(["list_gui_elements", "gui_elements", "list_gui_elems", "gui_elems"],
            args => Out(Log.DevCmd, win.renderer.guiGroups.FormatStr(" ;; ", g => $"[{g.name}: {g.allElements.FormatStr(", ", e => $"{e.name}")}]")),
            "LIST_GUI_ELEMENTS", [0]),

            new(["count_tick_listeners", "tick_listeners", "tick_lists"],
            args => Out(Log.DevCmd, $"{win.GetTickInvocationList().Length} listeners are subscribed to win.tick"),
            "COUNT_TICK_LISTENERS", [0]),

            new(["light_strength", "lstrength", "lightstr"],
            args => ParseNumber(args.First(), ref win.renderer.lightStrength),
            "LIGHT_STRENGTH <float>", [1]),

            new(["light_spacing", "lspacing", "lightspc"],
            args => ParseNumber(args.First(), ref (win.renderer.lightDistribution as GridLightDistribution).spacing),
            "LIGHT_SPACING <float>", [1]),

            new(["lighting", "light_switch"],
            args => ParseBool(args.FirstOrDefault(), ref win.renderer.lightingEnabled),
            "LIGHTING <on/off>", [0, 1])
        ];
    }


    /// <summary>Only really use for initializers, as it resizes the internal array</summary>
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
                OutErr(Log.DevCmd, exc);
            }
            finally
            {
                nextTick = null;
                Console.ForegroundColor = ConsoleColor.Gray;
            }
    }


    public void ParseBool(string strVal, ref bool target, bool throwExcIfFailed = true, bool logChange = true)
    {
        if(string.IsNullOrWhiteSpace(strVal))
            strVal = queryIfEmpty ? "?" : "^";

        switch(strVal.ToLower())
        {
            case "true" or "t" or "1" or "1b" or "yes" or "y" or "on" or "✅":
                target = true;
                break;
            case "false" or "f" or "0" or "0b" or "no" or "n" or "off" or "❌":
                target = false;
                break;
            case "switch" or "s" or "~" or "!" or "^" or "toggle" or "🔁":
                target ^= true;
                break;
            case "q" or "query" or "?":
                OutIf(Log.DevCmd, logChange, $"The current value is {target}");
                return;
            default:
                if(throwExcIfFailed)
                    throw new($"Invalid input for ParseBool: \"{strVal}\"");
                else
                    break;
        }

        OutIf(Log.DevCmd, logChange, $"The value is now set to {target}");
    }
    public void ParseBool(string strVal, Func<bool> get, Action<bool> set, bool throwExcIfFailed = true)
    {
        bool value = get();
        ParseBool(strVal, ref value, throwExcIfFailed);
        set(value);
    }
    public void ParseBool<T>(string strVal, T target, Expression<Func<T, bool>> getSet, bool throwExcIfFailed = true)
    {
        MemberExpression expr = getSet.Body as MemberExpression;
        PropertyInfo prop = expr.Member as PropertyInfo;
        bool value = (bool)prop.GetValue(target);
        ParseBool(strVal, ref value, throwExcIfFailed);
        prop.SetValue(target, value);
    }

    public static void ParseNumber<T>(string strVal, ref T field) where T : INumber<T>
    {
        if(string.IsNullOrWhiteSpace(strVal) || strVal is "q" or "query" or "?")
            Out(Log.DevCmd, $"The current value is {field}");
        else
            field = T.Parse(strVal, null);
    }
    public static void ParseNumber<T>(string strVal, Func<T> get, Action<T> set) where T : INumber<T>
    {
        T value = get();
        ParseNumber(strVal, ref value);
        set(value);
    }
    public static void ParseNumber<TTarget, TNumber>(string strVal, TTarget target, Expression<Func<TTarget, TNumber>> getSet) where TNumber : INumber<TNumber>
    {
        MemberExpression expr = getSet.Body as MemberExpression;
        PropertyInfo prop = expr.Member as PropertyInfo;
        TNumber value = (TNumber)prop.GetValue(target);
        ParseNumber(strVal, ref value);
        prop.SetValue(target, value);
    }

    public static void ParseVector<T>(string strVal, ref T field) where T : IVector<T>
        => field = T.Parse(strVal.Split(','));

    [GeneratedRegex("§f([0-9]+)")]
    private static partial Regex FgRegex();
    [GeneratedRegex("§b([0-9]+)")]
    private static partial Regex BgRegex();

    /// <summary>Format color using §fx (foreground), §bx (background), §r (reset) and §§ ('§' literal)</summary>
    public static string AddColor(string message, Color32[] fore = null, Color32[] back = null)
    {
        return AddColor(message,
            fore?.Select(c => $"\x1b[38;2;{c.r};{c.g};{c.b}m").ToArray() ?? [],
            back?.Select(c => $"\x1b[48;2;{c.r};{c.g};{c.b}m").ToArray() ?? []);
    }
    /// <summary>Format color using §fx (foreground), §bx (background), §r (reset) and §§ ('§' literal)</summary>
    public static string AddColor(string message, string[] fore, string[] back)
        => (message + "§r")
            .Replace(FgRegex(), m => fore[int.Parse(m.Groups[1].Value)])
            .Replace(BgRegex(), m => back[int.Parse(m.Groups[1].Value)])
            .Replace("§r", "\x1b[0m")
            .Replace("§§", "§");

    /// <summary>Format color using §fx (foreground), §bx (background), §r (reset) and §§ ('§' literal)</summary>
    public static void WriteLine(object message, Color32[] fore = null, Color32[] back = null, bool autoPrefixFB = true)
        => Console.WriteLine(AddColor(autoPrefixFB ? $"{(fore is null ? "" : "§f0")}{(back is null ? "" : "§b0")}{message}" : message.ToString(), fore, back));

    #region P/Invoke Interfacing
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
    #endregion


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

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleMode(nint hWnd, int mode);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetConsoleMode(nint hWnd, out int mode);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.SysInt)]
    private static partial nint GetStdHandle(int handle);
    #endregion
}