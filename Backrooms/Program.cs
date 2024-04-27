using System;

namespace Backrooms;

internal class Program
{
    private static void Main(string[] args)
    {
        Out("Press H to host, or J to join");
        char input = char.ToLower(Console.ReadLine()[0]);
        Assert(input is 'h' or 'j', "Input must be H or J");

        Game game;
        Window window = new(new(1920/6, 1080/6), "The Backrooms", "oli_appicon", true, w => game = new(w, input == 'h'));
    }
}