namespace SengokuScroll.Domain.Entities;

/// <summary>据点农业：分季进度、作型技术与劳力相关状态。</summary>
public sealed class StrongholdAgricultureState
{
    /// <summary>早稻/单季作进度（0–10000）。</summary>
    public int EarlyCycleProgressBp { get; set; }

    /// <summary>晚稻进度（0–10000）。</summary>
    public int LateCycleProgressBp { get; set; }

    /// <summary>第三季作进度（0–10000）。</summary>
    public int ThirdCycleProgressBp { get; set; }

    /// <summary>早稻进度上限（缺农时可能无法满收）。</summary>
    public int EarlyCycleProgressCapBp { get; set; } = 10_000;

    /// <summary>晚稻进度上限。</summary>
    public int LateCycleProgressCapBp { get; set; } = 10_000;

    /// <summary>第三季作进度上限。</summary>
    public int ThirdCycleProgressCapBp { get; set; } = 10_000;

    /// <summary>是否掌握二季作技术。</summary>
    public bool KnowsDoubleCrop { get; set; }

    /// <summary>是否掌握三季作技术。</summary>
    public bool KnowsTripleCrop { get; set; }

    public int GetProgressBp(int cycleIndex)
        => cycleIndex switch
        {
            0 => EarlyCycleProgressBp,
            1 => LateCycleProgressBp,
            2 => ThirdCycleProgressBp,
            _ => EarlyCycleProgressBp
        };

    public int GetProgressCapBp(int cycleIndex)
        => cycleIndex switch
        {
            0 => EarlyCycleProgressCapBp,
            1 => LateCycleProgressCapBp,
            2 => ThirdCycleProgressCapBp,
            _ => EarlyCycleProgressCapBp
        };

    public void SetProgressBp(int cycleIndex, int value)
    {
        value = Math.Clamp(value, 0, 10_000);
        switch (cycleIndex)
        {
            case 0: EarlyCycleProgressBp = value; break;
            case 1: LateCycleProgressBp = value; break;
            case 2: ThirdCycleProgressBp = value; break;
        }
    }

    public void SetProgressCapBp(int cycleIndex, int value)
    {
        value = Math.Clamp(value, 0, 10_000);
        switch (cycleIndex)
        {
            case 0: EarlyCycleProgressCapBp = value; break;
            case 1: LateCycleProgressCapBp = value; break;
            case 2: ThirdCycleProgressCapBp = value; break;
        }
    }

    public void ResetCycleProgress(int cycleIndex)
    {
        SetProgressBp(cycleIndex, 0);
        SetProgressCapBp(cycleIndex, 10_000);
    }
}
