using SengokuScroll.Common.Types;
using System.Runtime.CompilerServices;

namespace SengokuScroll.Domain.World;

public class TileMap
{
    private readonly byte[] terrain;

    private readonly byte[] region;

    public int Width { get; init; }

    public int Height { get; init; }

    public int Length { get; init; }

    public TileRef this[int index] => new(ref terrain[index], ref region[index]);

    public TileRef this[Point3 p] => this[GetIndex(p)];

    public TileMap(byte[] terrain, byte[] region, int width, int height)
    {
        Width = width;
        Height = height;
        Length = width * height;

        if (terrain.Length != Length)
            throw new ArgumentException("Length of Array is invalid.", nameof(terrain));

        if (region.Length != Length)
            throw new ArgumentException("Length of Array is invalid.", nameof(region));

        this.terrain = terrain;
        this.region = region;
    }

    public bool IsOutOfBounds(Point3 p) => p.Y < 0 || p.Y >= Height || p.X < 0 || p.X >= Width;

    public int GetIndex(Point3 p)
    {
        if (IsOutOfBounds(p))
            throw new ArgumentOutOfRangeException(nameof(p), $"TileMap index out of range: (y={p.Y}, x={p.X})");

        return GetIndexUnchecked(p);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetIndexUnchecked(Point3 p) => p.Y * Width + p.X;

    public ref byte GetTerrain(Point3 p) => ref terrain[GetIndex(p)];

    public ref byte GetRegion(Point3 p) => ref region[GetIndex(p)];

    public Point3 ToPoint3(int index)
    {
        if(index < 0 || index >= Length)
            throw new ArgumentOutOfRangeException(nameof(index), $"TileMap index out of range: index={index})");

        var w = Width;
        var y = index / w;
        var x = index % w;
        return new(x, y);
    }
}
