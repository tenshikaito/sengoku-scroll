using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data.Models;

namespace SengokuScroll.Strategy.Systems;

/// <summary>策略模式外交使节任务日推进接口。</summary>
public interface IStrategyDiplomacyMissionSystem : IGameSystem
{
}

/// <summary>
/// 日推进处理外交使节任务：行程递减、到期的成功率判定与关系变更。
/// 运行于势力 AI 之后、单位移动之前（Order 19）。
/// </summary>
public class StrategyDiplomacyMissionSystem(
    IGameContext context,
    StrategyScenarioMeta scenarioMeta) : IStrategyDiplomacyMissionSystem
{
    public int Order { get; } = 19;

    public void Update()
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;

        foreach (var diplomacy in gameData.Forces.Values.SelectMany(x => x.Diplomacies))
        {
            if (!diplomacy.IsTruce)
                continue;

            if (diplomacy.TrucePeriod > 0)
                diplomacy.TrucePeriod--;
            if (diplomacy.TrucePeriod == 0)
                diplomacy.IsTruce = false;
        }

        foreach (var character in context.GameWorldContext.EachCharacter())
        {
            if (character.IsDead || character.DiplomacyMission is null)
                continue;

            DiplomacyMissionActions.ProcessDailyMission(
                character,
                gameData,
                context.GameWorldContext.GameWorld.GameMasterData,
                scenarioMeta);
        }
    }
}
