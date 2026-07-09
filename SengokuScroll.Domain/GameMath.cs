using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Enums;

namespace SengokuScroll.Domain;

public static class GameMath
{
    public static Direction4 Opposite(Direction4 d)
    {
        return d switch
        {
            Direction4.Left => Direction4.Right,
            Direction4.Right => Direction4.Left,
            Direction4.Up => Direction4.Down,
            Direction4.Down => Direction4.Up,
            _ => Direction4.None
        };
    }

    public static Direction4 LocateAt4(Point2 me, Point2 target)
    {
        var delta = target - me;

        return delta switch
        {
            { X: < 0, Y: 0 } => Direction4.Left,
            { X: > 0, Y: 0 } => Direction4.Right,
            { X: 0, Y: < 0 } => Direction4.Up,
            { X: 0, Y: > 0 } => Direction4.Down,
            _ => Direction4.None
        };
    }

    public static Direction8 LocateAt8(Point2 me, Point2 target)
    {
        var delta = target - me;

        return delta switch
        {
            { X: < 0, Y: 0 } => Direction8.Left,
            { X: > 0, Y: 0 } => Direction8.Right,
            { X: 0, Y: < 0 } => Direction8.Up,
            { X: 0, Y: > 0 } => Direction8.Down,
            { X: < 0, Y: < 0 } => Direction8.LeftUp,
            { X: > 0, Y: < 0 } => Direction8.RightUp,
            { X: < 0, Y: > 0 } => Direction8.LeftDown,
            { X: > 0, Y: > 0 } => Direction8.RightDown,
            _ => Direction8.None
        };
    }
}
