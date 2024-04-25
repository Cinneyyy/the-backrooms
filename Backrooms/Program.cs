using System.Threading;

namespace Backrooms;

internal class Program
{
    private static void Main(string[] args)
    {
        while(!Resources.finishedInit)
            Thread.Sleep(10);

        Game game;
        Window window = new(new(1920/6, 1080/6), "The Backrooms", "oli_appicon", true, w => game = new(w));
    }
}