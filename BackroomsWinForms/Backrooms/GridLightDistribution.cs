namespace Backrooms;

public class GridLightDistribution(int spacing) : ILightDistribution
{
    public int spacing = spacing;


    public Vec2i ClosestLightSource(Vec2f pt)
        => (pt / spacing).Round() * spacing;

    public bool IsLightTile(Vec2i pt)
        => (pt + Vec2i.one) % spacing == Vec2i.zero;
}