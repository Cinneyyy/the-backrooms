global using static Backrooms.Debugging.Logger;
using System;

namespace Backrooms.Debugging;

public class Logger(Log log, Color32 color, bool enabled = true)
{
    public enum Log
    {
        Log = 0,
        DevCmd = 1,
        GameEvent = 2,
        Client = 3,
        Server = 4,
        MpManager = 5,
        Entity = 6,
        Debug = 7
    }


    private const int ErrBackground = 0x480000;
    private const int AssertBackground = 0x2d3000;

    public readonly string name = log.ToString();
    public readonly Color32 color = color;
    public bool enabled = enabled;

    /// <summary>Indices are values of the <see cref="Log"/> enum</summary>
    public static readonly Logger[] loggers = [
        new(Log.Log, new(0xbfbfbf)),
        new(Log.DevCmd, new(0x5465ff)),
        new(Log.GameEvent, new(0xd84dff)),
        new(Log.Client, new(0xf8ff3b)),
        new(Log.Server, new(0xfc8a26)),
        new(Log.MpManager, new(0xfc4823)),
        new(Log.Entity, new(0x3bffe8)),
        new(Log.Debug, new(0x35f0d7))
    ];


    public void Out(object message, string prefix = "[$n]")
    {
        if(enabled)
            DevConsole.WriteLine($"{prefix.Replace("$n", name)} {message}", fore: color);
    }

    public void OutErr(Exception exc, string format = "$e")
    {
        if(enabled)
#if DEBUG
            DevConsole.WriteLine($"[{name}] {format.Replace("$e", exc.ToString())}", fore: color, back: new(ErrBackground));
#else
            DevConsole.WriteLine($"[{name}] {format.Replace("$e", exc.Message)}", fore: color, back: new(ErrBackground));
#endif
    }

    public void OutIf(bool predicate, object message)
    {
        if(predicate)
            Out(message);
    }
    public void OutIf(bool predicate, Func<object> getMessage)
    {
        if(predicate)
            Out(getMessage());
    }

    public void Assert(bool assertion, object assertionFailed)
    {
        if(!assertion)
            DevConsole.WriteLine($"[{name}] {assertionFailed}", fore: color, back: new(AssertBackground));
    }
    public void Assert(bool assertion, Func<object> assertionFailed)
    {
        if(!assertion)
            DevConsole.WriteLine($"[{name}] {assertionFailed()}", fore: color, back: new(AssertBackground));
    }


    public static void Out(Logger logger, object message, string prefix = "[$n]")
        => logger.Out(message, prefix);
    public static void Out(Log logger, object message, string prefix = "[$n]")
        => loggers[(int)logger].Out(message, prefix);

    public static void OutErr(Logger logger, Exception exc, string format = "$e")
        => logger.OutErr(exc, format);
    public static void OutErr(Log logger, Exception exc, string format = "$e")
        => loggers[(int)logger].OutErr(exc, format);

    public static void OutIf(Logger logger, bool predicate, object message)
        => logger.OutIf(predicate, message);
    public static void OutIf(Logger logger, bool predicate, Func<object> getMessage)
        => logger.OutIf(predicate, getMessage);
    public static void OutIf(Log logger, bool predicate, object message)
        => loggers[(int)logger].OutIf(predicate, message);
    public static void OutIf(Log logger, bool predicate, Func<object> getMessage)
        => loggers[(int)logger].OutIf(predicate, getMessage);

    public static void Assert(Logger logger, bool assertion, object assertionFailed)
        => logger.Assert(assertion, assertionFailed);
    public static void Assert(Logger logger, bool assertion, Func<object> assertionFailed)
        => logger.Assert(assertion, assertionFailed);
    public static void Assert(Log logger, bool assertion, object assertionFailed)
        => loggers[(int)logger].Assert(assertion, assertionFailed);
    public static void Assert(Log logger, bool assertion, Func<object> assertionFailed)
        => loggers[(int)logger].Assert(assertion, assertionFailed);


    public static object operator +(Logger logger, object msg)
    {
        logger.Out(msg);
        return null;
    }
    public static object operator -(Logger logger, Exception exc)
    {
        logger.OutErr(exc);
        return null;
    }
}