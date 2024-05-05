using System;

namespace Backrooms;

internal class Program
{
    private static void Main(string[] args)
    {
        Out("Do you want to host? (y/n)");
        char input = char.ToLower(Console.ReadLine()[0]);

        Out("Where do you want to connect (e.g. 127.0.0.1:8080)");
        string[] endpoint = Console.ReadLine().Split(':');
        string ip = endpoint[0];
        int port = Convert.ToInt32(endpoint[1]);

        Out("What skin do you want? (1-5)");
        int skinIdx = Convert.ToInt32(Console.ReadLine()) - 1;

        Game game;
        Window window = new(new(1920/6, 1080/6), "The Backrooms", "oli_appicon", false, w => game = new(w, input == 'y', ip, port, skinIdx));
    }
}