namespace Backrooms;

/// <summary> All air tiles are even, all solid tiles are odd </summary>
public enum Tile : byte
{
    Air = 0,
    BigRoomAir = 2,
    PillarRoomAir = 4,

    Wall = 1,
    Pillar = 3
}