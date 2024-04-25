namespace Backrooms;

internal class Program
{
    private static void Main(string[] args)
    {
        Game game;
        Window window = new(new(1920/6, 1080/6), "The Backrooms", "oli", true, w => game = new(w));
    }
}