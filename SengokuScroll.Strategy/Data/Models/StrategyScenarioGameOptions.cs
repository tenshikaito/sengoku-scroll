namespace SengokuScroll.Strategy.Data.Models;

/// <summary>剧本/地图级规则选项（gameOptions）。</summary>
public sealed class StrategyScenarioGameOptions
{
    /// <summary>距当主居城每格行政效率损耗（百分点）；默认 1。</summary>
    public int AdministrativeEfficiencyLossPerTile { get; init; } = 1;
}
