namespace SengokuScroll.Common.Types;

public readonly struct Point3(int x, int y, int z = 0) : IEquatable<Point3>
{
    public int X { get; } = x;
    public int Y { get; } = y;
    public int Z { get; } = z;

    public static bool operator ==(Point3 left, Point3 right)
        => left.Equals(right);

    public static bool operator !=(Point3 left, Point3 right)
        => !left.Equals(right);

    public static Point3 operator -(Point3 p1, Point3 p2)
        => new(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);

    public static implicit operator Point2(Point3 p)
        => new(p.X, p.Y);

    public bool Equals(Point3 other)
        => X == other.X && Y == other.Y && Z == other.Z;

    public override bool Equals(object? obj)
        => obj is Point3 other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(X, Y, Z);

    public override string ToString()
        => $"({X}, {Y}, {Z})";
}