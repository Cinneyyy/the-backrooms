global using static Backrooms.Debugging.Logger;
using System;

namespace Backrooms.Debugging;

public static class Logger
{
    public static void Out(object message, ConsoleColor color = ConsoleColor.Gray)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
    }

    public static void OutErr(Exception exc, string format = "$e", ConsoleColor color = ConsoleColor.Red)
#if DEBUG
        => Out(format.Replace("$e", exc.ToString()), color);
#else
        => Out(format.Replace("$e", exc.Message), color);
#endif

    public static void Assert(bool assertion, string assertionFailedMsg, ConsoleColor color = ConsoleColor.Yellow)
    {
        if (!assertion)
            Out(assertionFailedMsg, color);
    }

    public static void ThrowIf(bool predicate, string message, ConsoleColor color = ConsoleColor.Red)
    {
        if (predicate)
        {
            Out(message, color);
            throw new(message);
        }
    }

    public static void OutIf(bool condition, object msg, ConsoleColor color = ConsoleColor.Gray)
    {
        if (condition)
            Out(msg, color);
    }
}