namespace Backrooms;

public class GridLightDistribution(int spacing) : ILightDistribution
{
    public int spacing = spacing;


    public float ClosestSqrLightDist(Vec2f pt)
    {
        Vec2f closestSrcRel = pt + Vec2f.half - (pt / spacing).Round() * spacing;
        return closestSrcRel.sqrLength;
    }

    public bool IsInLightTile(Vec2f pt)
        => pt.Ceil() % spacing == Vec2i.zero;
}