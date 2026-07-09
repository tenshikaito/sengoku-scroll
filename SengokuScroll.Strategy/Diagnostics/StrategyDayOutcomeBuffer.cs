using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>
/// 日推进结束后待呈现给玩家的结果缓冲。
/// 含：已结算战斗、信使抵达当主/部队等事件消息。
/// </summary>
public sealed class StrategyDayOutcomeBuffer
{
    private readonly List<StrategyBattleResultDto> resolvedBattles = [];
    private readonly List<StrategyEventDto> events = [];

    public IReadOnlyList<StrategyBattleResultDto> ResolvedBattles => resolvedBattles;

    public IReadOnlyList<StrategyEventDto> Events => events;

    public void AddBattle(StrategyBattleResultDto result) => resolvedBattles.Add(result);

    public void AddEvent(StrategyEventDto evt) => events.Add(evt);

    public void Clear()
    {
        resolvedBattles.Clear();
        events.Clear();
    }
}
