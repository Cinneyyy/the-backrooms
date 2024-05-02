using System;

namespace Backrooms;

internal class Program
{
    private static void Main(string[] args)
    {
        Out("Do you want to host? (y/n)");
        char input = char.ToLower(Console.ReadLine()[0]);

        Game game;
        Window window = new(new(1920/6, 1080/6), "The Backrooms", "oli_appicon", false, w => game = new(w, input == 'y'));
    }
}