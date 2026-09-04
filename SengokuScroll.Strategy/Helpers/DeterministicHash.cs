namespace SengokuScroll.Strategy.Helpers;

/// <summary>
/// 跨进程稳定的 FNV-1a 哈希。不要用 <see cref="HashCode"/> 生成回放/联机随机种子，
/// 因为它会在不同进程使用随机盐。
/// </summary>
public static class DeterministicHash
{
    private const uint OffsetBasis = 2166136261;
    private const uint Prime = 16777619;

    public static int Combine(params int[] values)
    {
        var hash = OffsetBasis;
        foreach (var value in values)
            hash = AddInt32(hash, value);
        return Positive(hash);
    }

    public static int Combine(string? text, params int[] values)
    {
        var hash = OffsetBasis;
        if (text is not null)
        {
            foreach (var character in text)
            {
                hash = AddByte(hash, (byte)character);
                hash = AddByte(hash, (byte)(character >> 8));
            }
        }

        hash = AddByte(hash, 0xff);
        foreach (var value in values)
            hash = AddInt32(hash, value);
        return Positive(hash);
    }

    private static uint AddInt32(uint hash, int value)
    {
        var raw = unchecked((uint)value);
        for (var shift = 0; shift < 32; shift += 8)
            hash = AddByte(hash, (byte)(raw >> shift));
        return hash;
    }

    private static uint AddByte(uint hash, byte value)
        => unchecked((hash ^ value) * Prime);

    private static int Positive(uint hash)
    {
        var value = (int)(hash & 0x7fff_ffff);
        return value == 0 ? 1 : value;
    }
}
