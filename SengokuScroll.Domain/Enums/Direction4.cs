namespace SengokuScroll.Domain.Enums;

public enum Direction4 : byte
{
    None = 0,
    Left = 1 << 1,
    Right = 1 << 2,
    Up = 1 << 3,
    Down = 1 << 4
}
