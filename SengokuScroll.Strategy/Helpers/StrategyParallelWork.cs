namespace SengokuScroll.Strategy.Helpers;

/// <summary>
/// 策略仿真的受控并行入口。仅用于互不写共享世界的纯计算；结果按输入索引保存，
/// 后续仍由调用方以固定顺序提交，保证存档、回放和测试的确定性。
/// </summary>
public static class StrategyParallelWork
{
    /// <summary>为系统与前端保留一个逻辑处理器，避免长局计算占满线程池。</summary>
    public static int MaxDegreeOfParallelism
        => Math.Max(1, Environment.ProcessorCount - 1);

    public static TResult[] MapOrdered<TSource, TResult>(
        IReadOnlyList<TSource> source,
        Func<TSource, TResult> selector,
        int minimumParallelCount = 32)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var results = new TResult[source.Count];
        if (source.Count == 0)
            return results;

        if (source.Count < minimumParallelCount || MaxDegreeOfParallelism <= 1)
        {
            for (var index = 0; index < source.Count; index++)
                results[index] = selector(source[index]);
            return results;
        }

        Parallel.For(
            0,
            source.Count,
            new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism },
            index => results[index] = selector(source[index]));
        return results;
    }

    public static void ForEachIndex(int count, Action<int> action, int minimumParallelCount = 32)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentNullException.ThrowIfNull(action);

        if (count < minimumParallelCount || MaxDegreeOfParallelism <= 1)
        {
            for (var index = 0; index < count; index++)
                action(index);
            return;
        }

        Parallel.For(
            0,
            count,
            new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism },
            action);
    }
}
