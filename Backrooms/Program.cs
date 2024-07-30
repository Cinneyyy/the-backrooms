namespace Backrooms;

public class Program
{
    public static Window window { get; private set; }


    private static void Main(string[] args)
    {
        window = new(new Vec2i(1920, 1080) / 6, "The Backrooms", args, "oli_appicon", false, false, w => _ = new Game(w));
    }
}