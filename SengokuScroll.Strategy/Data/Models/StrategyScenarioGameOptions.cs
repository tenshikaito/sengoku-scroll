namespace SengokuScroll.Strategy.Data.Models;

/// <summary>剧本/地图级规则选项（gameOptions）。</summary>
public sealed class StrategyScenarioGameOptions
{
    /// <summary>距当主居城每格行政效率损耗（百分点）；默认 1。</summary>
    public int AdministrativeEfficiencyLossPerTile { get; init; } = 1;

    /// <summary>
    /// 非史实（无地标背书）据点收入系数（万分比）；默认 8000 = 80%。
    /// 不随难度变化；开局选项不再覆盖此项。
    /// </summary>
    public int FictionalIncomePenaltyBp { get; init; } = 8000;
}
