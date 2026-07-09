namespace SengokuScroll.Common.Types;

public enum CardinalPattern : byte
{
    None = 0,
    North = 1,
    East = 1 << 1,
    South = 1 << 2,
    West = 1 << 3,
    NorthEast = North | East,
    NorthSouth = North | South,
    NorthWest = North | West,
    EastSouth = East | South,
    EastWest = East | West,
    SouthWest = South | West,
    NorthEastSouth = North | East | South,
    NorthEastWest = North | East | West,
    NorthSouthWest = North | South | West,
    EastSouthWest = East | South | West,
    All = North | East | South | West
}