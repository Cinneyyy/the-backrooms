using System;

namespace Backrooms;

internal class Program
{
    private static void Main(string[] args)
    {
        Out("Do you want to host? (y/n)");
        string hostStr = Console.ReadLine();
        bool host = string.IsNullOrWhiteSpace(hostStr) || char.ToLower(hostStr[0]) == 'y';

        Out("Where do you want to connect (e.g. 127.0.0.1:8080)");
        string endpointStr = Console.ReadLine();
        string[] endpointSplit = endpointStr?.Split(':');
        (string ip, int port) = string.IsNullOrWhiteSpace(endpointStr) ? ("localhost", 8080) : (endpointSplit[0], Convert.ToInt32(endpointSplit[1]));

        Out("What skin do you want? (1. hazmat_suit, 2. entity, 3. freddy_fazbear, 4. huggy_wuggy, 5. purple_guy)");
        string skinStr = Console.ReadLine();
        int skinIdx = string.IsNullOrWhiteSpace(skinStr) ? 0 : Convert.ToInt32(skinStr) - 1;

        Window window = new(new(1920/6, 1080/6), "The Backrooms", "oli_appicon", false, w => _ = new Game(w, host, ip, port, skinIdx));
    }
}