namespace Backrooms;

/// <summary> All air tiles are even, all solid tiles are odd </summary>
public enum Tile : byte
{
    Void = 0,

    Air = 2,
    BigRoomAir = 4,
    PillarRoomAir = 6,

    Wall = 1,
    Pillar = 3
}