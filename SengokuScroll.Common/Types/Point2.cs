namespace SengokuScroll.Common.Types;

public readonly struct Point2(int x, int y) : IEquatable<Point2>
{
    public int X { get; } = x;

    public int Y { get; } = y;

    public static Point2 Zero => new(0, 0);

    public static bool operator ==(Point2 left, Point2 right)
        => left.Equals(right);

    public static bool operator !=(Point2 left, Point2 right)
        => !left.Equals(right);

    public static Point2 operator -(Point2 p1, Point2 p2)
        => new(p1.X - p2.X, p1.Y - p2.Y);

    public static implicit operator Point3(Point2 point)
        => new(point.X, point.Y);

    public bool Equals(Point2 other)
        => X == other.X && Y == other.Y;

    public override bool Equals(object? obj)
        => obj is Point2 other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(X, Y);

    public override string ToString()
        => $"({X}, {Y})";
}