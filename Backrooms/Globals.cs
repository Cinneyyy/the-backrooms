﻿global using static Backrooms.Globals;
using System;

namespace Backrooms;

public static class Globals
{
    public static void Out(object message, ConsoleColor color = ConsoleColor.Gray)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
    }

    public static void OutErr(Exception exc, ConsoleColor color = ConsoleColor.Red)
        => Out(exc, color);

    public static void Assert(bool assertion, string assertionFailedMsg, ConsoleColor color = ConsoleColor.Red)
    {
        if(!assertion)
            Out(assertionFailedMsg, color);
    }

    public static void OutIf(bool condition, object msg, ConsoleColor color = ConsoleColor.Gray)
    {
        if(condition)
            Out(msg, color);
    }
}