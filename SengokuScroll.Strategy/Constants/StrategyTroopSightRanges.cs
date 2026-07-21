namespace SengokuScroll.Strategy.Constants;

/// <summary>兵种默认视野半径（曼哈顿距离 |dx|+|dy| ≤ range）。</summary>
public static class StrategyTroopSightRanges
{
    public const int Default = 2;

    public static int Resolve(int typeId) => Default;
}

/// <summary>据点默认视野半径。</summary>
public static class StrategyStrongholdSightRanges
{
    public const int Default = 2;
}
