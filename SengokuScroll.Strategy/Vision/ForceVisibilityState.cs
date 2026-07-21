namespace SengokuScroll.Strategy.Vision;

/// <summary>某势力持久探索态 + 当日可见格 + 已知据点。</summary>
public sealed class ForceVisibilityState
{
    private bool[] explored = [];

    public HashSet<(int X, int Y)> VisibleCells { get; } = [];

    public HashSet<int> KnownStrongholdIds { get; } = [];

    public void EnsureCapacity(int width, int height)
    {
        var size = Math.Max(1, width * height);
        if (explored.Length == size)
            return;

        var next = new bool[size];
        if (explored.Length > 0)
            Array.Copy(explored, next, Math.Min(explored.Length, next.Length));

        explored = next;
    }

    public bool IsExplored(int x, int y, int width)
    {
        var index = y * width + x;
        return index >= 0 && index < explored.Length && explored[index];
    }

    public void MarkExplored(int x, int y, int width)
    {
        var index = y * width + x;
        if (index < 0 || index >= explored.Length)
            return;

        explored[index] = true;
    }

    public void MarkExplored(IEnumerable<(int X, int Y)> cells, int width)
    {
        foreach (var (x, y) in cells)
            MarkExplored(x, y, width);
    }

    public IReadOnlyList<uint> PackExploredBits()
    {
        if (explored.Length == 0)
            return [];

        var words = (explored.Length + 31) / 32;
        var result = new uint[words];
        for (var i = 0; i < explored.Length; i++)
        {
            if (!explored[i])
                continue;

            result[i >> 5] |= 1u << (i & 31);
        }

        return result;
    }

    public void UnpackExploredBits(IReadOnlyList<uint> bits, int width, int height)
    {
        EnsureCapacity(width, height);
        Array.Clear(explored);

        for (var i = 0; i < explored.Length; i++)
        {
            var word = bits[i >> 5];
            explored[i] = ((word >> (i & 31)) & 1u) != 0;
        }
    }
}
