namespace Backrooms;

internal class Program
{
    private static void Main(string[] args)
    {
        Window window = new(new Vec2i(1920, 1080) / 6, "The Backrooms", "oli_appicon", false, false, w => _ = new Game(w));
    }
}