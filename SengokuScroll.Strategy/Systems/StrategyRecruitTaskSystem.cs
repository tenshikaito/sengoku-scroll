using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;

namespace SengokuScroll.Strategy.Systems;

/// <summary>策略模式募兵/征兵任务日推进接口。</summary>
public interface IStrategyRecruitTaskSystem : IGameSystem
{
}

/// <summary>
/// 日推进处理将领募兵/征兵任务：期限递减、执行募兵/征兵、回城汇报结算。
/// 运行于角色 AI 之前（Order 16）。
/// </summary>
public class StrategyRecruitTaskSystem(
    IGameContext context,
    StrategyScenarioMeta scenarioMeta,
    StrategyDayOutcomeBuffer dayOutcomeBuffer) : IStrategyRecruitTaskSystem
{
    public int Order { get; } = 16;

    public void Update()
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;

        foreach (var character in context.GameWorldContext.EachCharacter())
        {
            if (character.IsDead)
                continue;

            if (character.RecruitAssignment is not null)
                StrongholdRecruitTaskActions.TryBeginAssignedRecruitExecution(character, gameData, scenarioMeta);

            if (character.RecruitTask is null)
                continue;

            StrongholdRecruitTaskActions.ProcessDailyTask(character, gameData, scenarioMeta, dayOutcomeBuffer);
        }
    }
}
