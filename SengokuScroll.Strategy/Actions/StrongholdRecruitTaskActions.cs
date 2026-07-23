using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Actions;

/// <summary>据点募兵/征兵任务派发与结算。</summary>
public static class StrongholdRecruitTaskActions
{
    public static GameError? TryAssignMercenaryRecruitTask(
        Stronghold stronghold,
        int characterId,
        int budgetMoney,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        if (!TryResolveAssignableGeneral(stronghold, characterId, gameData, meta, out var character, out var generalError))
            return generalError;

        if (!StrongholdRecruitTaskRules.TryValidateStrongholdMercenaryBudget(stronghold, budgetMoney, out var budgetError))
            return budgetError;

        stronghold.ForceActor.Money -= budgetMoney;

        PublishRecruitAssignment(
            character,
            stronghold,
            new CharacterRecruitAssignment
            {
                Kind = CharacterRecruitTaskKind.Mercenary,
                StrongholdId = stronghold.Id,
                BudgetMoney = budgetMoney,
            });

        return null;
    }

    public static GameError? TryAssignPersonalMercenaryRecruitTask(
        Stronghold stronghold,
        Character character,
        int budgetMoney,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        if (!StrongholdRecruitTaskRules.CanExecutePersonalStrongholdCommands(character, stronghold, meta, gameData))
            return GameError.DomesticError.CharacterNotAtStronghold;

        if (!StrongholdRecruitTaskRules.TryValidatePersonalMercenaryBudget(character, budgetMoney, out var budgetError))
            return budgetError;

        character.Money -= budgetMoney;

        BeginRecruitTask(
            character,
            stronghold,
            gameData,
            meta,
            new CharacterRecruitTask
            {
                Kind = CharacterRecruitTaskKind.Mercenary,
                StrongholdId = stronghold.Id,
                BudgetMoney = budgetMoney,
                MoneyRemaining = budgetMoney,
                UsesPersonalFunds = true,
                DeadlineDaysRemaining = RecruitConstants.TaskDeadlineDays,
                ExecutionDaysRemaining = RecruitConstants.ExecutionDays,
            });

        return null;
    }

    public static GameError? TryAssignPersonalConscriptRecruitTask(
        Stronghold stronghold,
        Character character,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        if (!StrongholdRecruitTaskRules.TryValidateConscriptCapacity(stronghold, out var capacityError))
            return capacityError;

        if (!StrongholdRecruitTaskRules.CanExecutePersonalStrongholdCommands(character, stronghold, meta, gameData))
            return GameError.DomesticError.CharacterNotAtStronghold;

        BeginRecruitTask(
            character,
            stronghold,
            gameData,
            meta,
            new CharacterRecruitTask
            {
                Kind = CharacterRecruitTaskKind.Conscript,
                StrongholdId = stronghold.Id,
                DeadlineDaysRemaining = RecruitConstants.TaskDeadlineDays,
                ExecutionDaysRemaining = RecruitConstants.ExecutionDays,
            });

        return null;
    }

    public static GameError? TryAssignConscriptRecruitTask(
        Stronghold stronghold,
        int characterId,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        if (!StrongholdRecruitTaskRules.TryValidateConscriptCapacity(stronghold, out var capacityError))
            return capacityError;

        if (!TryResolveAssignableGeneral(stronghold, characterId, gameData, meta, out var character, out var generalError))
            return generalError;

        PublishRecruitAssignment(
            character,
            stronghold,
            new CharacterRecruitAssignment
            {
                Kind = CharacterRecruitTaskKind.Conscript,
                StrongholdId = stronghold.Id,
            });

        return null;
    }

    /// <summary>角色抵达任务目标据点后，将任务令转为个人执行任务。</summary>
    public static void TryBeginAssignedRecruitExecution(
        Character character,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        var assignment = character.RecruitAssignment;
        if (assignment is null || character.RecruitTask is not null)
            return;

        if (!gameData.Strongholds.TryGetValue(assignment.StrongholdId, out var stronghold))
        {
            AbortAssignment(character, gameData);
            return;
        }

        if (!CharacterAiRules.IsAtStronghold(character, stronghold))
            return;

        if (assignment.Kind == CharacterRecruitTaskKind.Conscript
            && !StrongholdRecruitTaskRules.TryValidateConscriptCapacity(stronghold, out _))
        {
            AbortAssignment(character, gameData);
            return;
        }

        character.RecruitAssignment = null;

        var task = assignment.Kind switch
        {
            CharacterRecruitTaskKind.Mercenary => new CharacterRecruitTask
            {
                Kind = CharacterRecruitTaskKind.Mercenary,
                StrongholdId = stronghold.Id,
                BudgetMoney = assignment.BudgetMoney,
                MoneyRemaining = assignment.BudgetMoney,
                UsesPersonalFunds = false,
                DeadlineDaysRemaining = RecruitConstants.TaskDeadlineDays,
                ExecutionDaysRemaining = RecruitConstants.ExecutionDays,
                Phase = CharacterRecruitTaskPhase.Execute,
            },
            CharacterRecruitTaskKind.Conscript => new CharacterRecruitTask
            {
                Kind = CharacterRecruitTaskKind.Conscript,
                StrongholdId = stronghold.Id,
                DeadlineDaysRemaining = RecruitConstants.TaskDeadlineDays,
                ExecutionDaysRemaining = RecruitConstants.ExecutionDays,
                Phase = CharacterRecruitTaskPhase.Execute,
            },
            _ => null,
        };

        if (task is null)
            return;

        BeginRecruitTask(character, stronghold, gameData, meta, task);
    }

    public static void ProcessDailyTask(
        Character character,
        GameData gameData,
        StrategyScenarioMeta? meta = null,
        StrategyDayOutcomeBuffer? dayOutcomeBuffer = null)
    {
        var task = character.RecruitTask;
        if (task is null)
            return;

        task.DeadlineDaysRemaining = Math.Max(0, task.DeadlineDaysRemaining - 1);

        if (!gameData.Strongholds.TryGetValue(task.StrongholdId, out var targetStronghold))
        {
            AbortTask(character);
            return;
        }

        if (task.DeadlineDaysRemaining <= 0 && task.Phase != CharacterRecruitTaskPhase.Report)
            BeginReportPhase(character, task);

        switch (task.Phase)
        {
            case CharacterRecruitTaskPhase.Travel:
                ProcessTravelPhase(character, task, targetStronghold);
                if (task.Phase == CharacterRecruitTaskPhase.Execute)
                    ProcessExecutePhase(character, task, targetStronghold);
                if (task.Phase == CharacterRecruitTaskPhase.Report)
                    ProcessReportPhase(character, task, gameData, meta, dayOutcomeBuffer);
                break;
            case CharacterRecruitTaskPhase.Execute:
                ProcessExecutePhase(character, task, targetStronghold);
                if (task.Phase == CharacterRecruitTaskPhase.Report)
                    ProcessReportPhase(character, task, gameData, meta, dayOutcomeBuffer);
                break;
            case CharacterRecruitTaskPhase.Report:
                ProcessReportPhase(character, task, gameData, meta, dayOutcomeBuffer);
                break;
        }
    }

    public static void CompleteTask(
        Character character,
        GameData gameData,
        StrategyScenarioMeta? meta = null,
        StrategyDayOutcomeBuffer? dayOutcomeBuffer = null)
    {
        var task = character.RecruitTask;
        if (task is null)
            return;

        if (gameData.Strongholds.TryGetValue(task.StrongholdId, out var stronghold))
        {
            PublishRecruitTaskCompletedEvent(character, task, stronghold, meta, dayOutcomeBuffer);

            if (task.SoldiersRecruited > 0)
                stronghold.ForceActor.Soldier += task.SoldiersRecruited;

            if (task.Kind == CharacterRecruitTaskKind.Mercenary && task.MoneyRemaining > 0)
            {
                if (task.UsesPersonalFunds)
                    character.Money += task.MoneyRemaining;
                else
                    stronghold.ForceActor.Money += task.MoneyRemaining;
            }

            if (gameData.Forces.TryGetValue(stronghold.ForceId, out var force))
                ForceEconomyActions.SyncForceTreasuryFromStrongholds(force, gameData);
        }

        character.Popular = Math.Min(100, character.Popular + RecruitConstants.MeritRewardOnComplete);
        ClearTaskState(character);
    }

    private static void ProcessTravelPhase(
        Character character,
        CharacterRecruitTask task,
        Stronghold targetStronghold)
    {
        if (!CharacterAiRules.IsAtStronghold(character, targetStronghold))
            return;

        task.Phase = CharacterRecruitTaskPhase.Execute;
        character.ActionStatus = CharacterActionStatus.Acting;
    }

    private static void ProcessExecutePhase(
        Character character,
        CharacterRecruitTask task,
        Stronghold targetStronghold)
    {
        if (character.ActionStatus != CharacterActionStatus.Acting)
            character.ActionStatus = CharacterActionStatus.Acting;

        if (task.ExecutionDaysRemaining <= 0)
        {
            BeginReportPhase(character, task);
            return;
        }

        _ = task.Kind switch
        {
            CharacterRecruitTaskKind.Mercenary => ProcessMercenaryExecutionDay(task, targetStronghold),
            CharacterRecruitTaskKind.Conscript => ProcessConscriptExecutionDay(task, targetStronghold, character),
            _ => 0,
        };

        task.ExecutionDaysRemaining = Math.Max(0, task.ExecutionDaysRemaining - 1);

        var moneyExhausted = task.Kind == CharacterRecruitTaskKind.Mercenary && task.MoneyRemaining <= 0;
        if (task.ExecutionDaysRemaining <= 0 || moneyExhausted)
            BeginReportPhase(character, task);
    }

    private static void ProcessReportPhase(
        Character character,
        CharacterRecruitTask task,
        GameData gameData,
        StrategyScenarioMeta? meta,
        StrategyDayOutcomeBuffer? dayOutcomeBuffer)
    {
        character.ActionPlan = CharacterActionPlan.Report;
        character.ForceStatus = CharacterForceStatus.Task;
        character.ActionTarget.StrongholdId = task.ReportStrongholdId;
        character.ActionTarget.RoutePoints.Clear();

        if (task.ReportStrongholdId <= 0
            || !gameData.Strongholds.TryGetValue(task.ReportStrongholdId, out var residence))
        {
            CompleteTask(character, gameData, meta, dayOutcomeBuffer);
            return;
        }

        if (!CharacterAiRules.IsAtStronghold(character, residence)
            || character.ActionStatus == CharacterActionStatus.Moving)
        {
            return;
        }

        CompleteTask(character, gameData, meta, dayOutcomeBuffer);
    }

    private static void BeginReportPhase(Character character, CharacterRecruitTask task)
    {
        task.Phase = CharacterRecruitTaskPhase.Report;
        character.ActionPlan = CharacterActionPlan.Report;
        character.ActionTarget.StrongholdId = task.ReportStrongholdId;
        character.ActionTarget.RoutePoints.Clear();
        character.ActionStatus = CharacterActionStatus.Waiting;
    }

    private static int ProcessMercenaryExecutionDay(CharacterRecruitTask task, Stronghold stronghold)
    {
        if (task.MoneyRemaining < RecruitConstants.MoneyCostPerSoldier)
            return 0;

        var affordable = RecruitConstants.SoldiersAffordableByMoney(task.MoneyRemaining);
        var targetTotal = RecruitConstants.SoldiersAffordableByMoney(task.BudgetMoney);
        var perDayCap = Math.Max(
            1,
            (targetTotal + RecruitConstants.ExecutionDays - 1) / RecruitConstants.ExecutionDays);
        var byPopulation = stronghold.Population / RecruitConstants.PopulationCostPerSoldier;
        var soldiers = Math.Min(affordable, Math.Min(perDayCap, byPopulation));
        if (soldiers <= 0)
            return 0;

        var cost = soldiers * RecruitConstants.MoneyCostPerSoldier;
        task.MoneyRemaining -= cost;
        task.SoldiersRecruited += soldiers;
        stronghold.Population -= soldiers * RecruitConstants.PopulationCostPerSoldier;
        return soldiers;
    }

    private static int ProcessConscriptExecutionDay(
        CharacterRecruitTask task,
        Stronghold stronghold,
        Character character)
    {
        var byPopulation = stronghold.Population / RecruitConstants.PopulationCostPerSoldier;
        var perDayCap = Math.Max(1, CharacterEffectiveStats.Charm(character) / RecruitConstants.ConscriptCharmDivisor);
        var soldiers = Math.Min(byPopulation, perDayCap);
        if (soldiers <= 0)
            return 0;

        task.SoldiersRecruited += soldiers;
        stronghold.Population -= soldiers * RecruitConstants.PopulationCostPerSoldier;

        var popularPenalty = Math.Max(
            1,
            soldiers / RecruitConstants.ConscriptPopularFeelingsSoldiersPerPoint);
        var stabilityPenalty = Math.Max(
            1,
            soldiers / RecruitConstants.ConscriptStabilitySoldiersPerPoint);
        stronghold.CivilianActor.PopularFeelings = (byte)Math.Max(
            0,
            stronghold.CivilianActor.PopularFeelings - popularPenalty);
        stronghold.Stability = (byte)Math.Max(0, stronghold.Stability - stabilityPenalty);
        return soldiers;
    }

    private static void PublishRecruitAssignment(
        Character character,
        Stronghold stronghold,
        CharacterRecruitAssignment assignment)
    {
        character.RecruitAssignment = assignment;
        CharacterAiMovementHelper.ScheduleGovernanceTravelTask(character, stronghold);
    }

    private static void AbortAssignment(Character character, GameData gameData)
    {
        RefundRecruitAssignmentBudget(character, gameData);
        ClearRecruitAssignmentState(character);
    }

    /// <summary>召回时取消尚未开始的募兵/征兵任务令并退回预算。</summary>
    public static void AbortRecruitAssignmentForRecall(Character character, GameData gameData)
        => AbortAssignment(character, gameData);

    /// <summary>
    /// 召回时结算进行中的募兵/征兵任务：未用资金退回，已获兵士效果减半。
    /// 返回将领应收队据点 Id。
    /// </summary>
    public static int SettleRecruitTaskOnRecall(Character character, GameData gameData, StrategyScenarioMeta meta)
    {
        var task = character.RecruitTask
            ?? throw new InvalidOperationException("Character has no recruit task.");

        var returnStrongholdId = task.ReportStrongholdId > 0
            ? task.ReportStrongholdId
            : StrategyLordHelper.ResolveLordResidenceStrongholdId(character.ForceId, gameData, meta);

        if (gameData.Strongholds.TryGetValue(task.StrongholdId, out var stronghold))
        {
            if (task.Phase is CharacterRecruitTaskPhase.Execute or CharacterRecruitTaskPhase.Report)
            {
                var effectiveSoldiers = task.SoldiersRecruited / 2;
                if (effectiveSoldiers > 0)
                    stronghold.ForceActor.Soldier += effectiveSoldiers;
            }

            if (task.Kind == CharacterRecruitTaskKind.Mercenary && task.MoneyRemaining > 0)
            {
                if (task.UsesPersonalFunds)
                    character.Money += task.MoneyRemaining;
                else
                    stronghold.ForceActor.Money += task.MoneyRemaining;
            }

            if (gameData.Forces.TryGetValue(stronghold.ForceId, out var force))
                ForceEconomyActions.SyncForceTreasuryFromStrongholds(force, gameData);
        }

        ClearTaskState(character);
        return returnStrongholdId;
    }

    private static void RefundRecruitAssignmentBudget(Character character, GameData gameData)
    {
        var assignment = character.RecruitAssignment;
        if (assignment is null)
            return;

        if (assignment.Kind == CharacterRecruitTaskKind.Mercenary
            && assignment.BudgetMoney > 0
            && gameData.Strongholds.TryGetValue(assignment.StrongholdId, out var stronghold))
        {
            stronghold.ForceActor.Money += assignment.BudgetMoney;
            if (gameData.Forces.TryGetValue(stronghold.ForceId, out var force))
                ForceEconomyActions.SyncForceTreasuryFromStrongholds(force, gameData);
        }
    }

    private static void ClearRecruitAssignmentState(Character character)
    {
        character.RecruitAssignment = null;
        character.ForceStatus = CharacterForceStatus.Idle;
        character.ActionPlan = CharacterActionPlan.Rest;
        character.ActionStatus = CharacterActionStatus.Waiting;
        character.ActionTarget.StrongholdId = 0;
        character.ActionTarget.RoutePoints.Clear();
    }

    private static void BeginRecruitTask(
        Character character,
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta,
        CharacterRecruitTask task)
    {
        task.ReportStrongholdId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
            stronghold.ForceId,
            gameData,
            meta);
        if (task.Phase != CharacterRecruitTaskPhase.Execute)
            task.Phase = CharacterRecruitTaskPhase.Travel;

        character.RecruitTask = task;
        character.ForceStatus = CharacterForceStatus.Task;
        character.ActionPlan = CharacterActionPlan.Task;
        character.ActionTarget.StrongholdId = stronghold.Id;
        character.ActionTarget.RoutePoints.Clear();
        character.ActionStatus = task.Phase == CharacterRecruitTaskPhase.Execute
            ? CharacterActionStatus.Acting
            : CharacterActionStatus.Waiting;
        character.LastAiCheckDate = default;

        if (task.Phase == CharacterRecruitTaskPhase.Travel
            && CharacterAiRules.IsAtStronghold(character, stronghold))
        {
            task.Phase = CharacterRecruitTaskPhase.Execute;
            character.ActionStatus = CharacterActionStatus.Acting;
        }
    }

    private static bool TryResolveAssignableGeneral(
        Stronghold stronghold,
        int characterId,
        GameData gameData,
        StrategyScenarioMeta meta,
        out Character character,
        out GameError? error)
    {
        character = null!;
        error = null;

        if (characterId <= 0 || !gameData.Characters.TryGetValue(characterId, out var resolved) || resolved.IsDead)
        {
            error = GameError.DataNotFound;
            return false;
        }

        if (resolved.ForceId != stronghold.ForceId)
        {
            error = GameError.DiplomacyError.NotSelfForce;
            return false;
        }

        if (!StrongholdRecruitTaskRules.IsIdleGeneralAtStronghold(resolved, stronghold.Id))
        {
            error = resolved.RecruitTask is not null
                    || resolved.RecruitAssignment is not null
                    || resolved.ForceStatus != CharacterForceStatus.Idle
                ? GameError.DomesticError.CharacterHasActiveTask
                : GameError.DomesticError.CharacterNotAtStronghold;
            return false;
        }

        character = resolved;
        return true;
    }

    private static void AbortTask(Character character)
        => ClearTaskState(character);

    private static void ClearTaskState(Character character)
    {
        character.RecruitTask = null;
        character.ForceStatus = CharacterForceStatus.Idle;
        character.ActionPlan = CharacterActionPlan.Rest;
        character.ActionStatus = CharacterActionStatus.Waiting;
        character.ActionTarget.StrongholdId = 0;
        character.ActionTarget.RoutePoints.Clear();
    }

    private static void PublishRecruitTaskCompletedEvent(
        Character character,
        CharacterRecruitTask task,
        Stronghold stronghold,
        StrategyScenarioMeta? meta,
        StrategyDayOutcomeBuffer? dayOutcomeBuffer)
    {
        if (meta is null || dayOutcomeBuffer is null || character.ForceId != meta.PlayerForceId)
            return;

        var kindLabel = task.Kind == CharacterRecruitTaskKind.Mercenary ? "募兵" : "征兵";
        var actionVerb = task.Kind == CharacterRecruitTaskKind.Mercenary ? "募集" : "征募";
        var formalTitle = task.SoldiersRecruited > 0
            ? $"{character.Name}在{stronghold.Name}{actionVerb}了士兵 {task.SoldiersRecruited:N0} 人。"
            : $"{character.Name}在{stronghold.Name}的{kindLabel}任务未能{actionVerb}到兵士。";

        var informalBrief = task.SoldiersRecruited > 0
            ? $"主公，{stronghold.Name}的{kindLabel}完成了！共得 {task.SoldiersRecruited:N0} 人。"
            : $"主公，{stronghold.Name}的{kindLabel}任务已结束，未能募得兵士。";

        var detailLines = new List<string>
        {
            $"📋 {character.Name} 已完成 {stronghold.Name} {kindLabel}任务",
            $"获得兵士 {task.SoldiersRecruited} 人",
            $"据点现有兵士 {stronghold.ForceActor.Soldier + task.SoldiersRecruited} 人",
        };

        if (task.Kind == CharacterRecruitTaskKind.Mercenary)
        {
            detailLines.Add($"任务预算 {FormatMoneyKan(task.BudgetMoney)}");
            if (task.MoneyRemaining > 0)
            {
                var refundTarget = task.UsesPersonalFunds ? "个人金库" : "据点府库";
                detailLines.Add($"剩余 {FormatMoneyKan(task.MoneyRemaining)} 已退回{refundTarget}");
            }
        }

        dayOutcomeBuffer.AddEvent(new StrategyEventDto
        {
            Category = "RecruitTaskCompleted",
            Title = formalTitle,
            Message = string.Join('\n', detailLines),
            Brief = informalBrief,
            DetailMessage = string.Join('\n', detailLines),
            DetailCategory = task.Kind.ToString(),
            CharacterId = character.Id,
            CharacterName = character.Name,
        });
    }

    private static string FormatMoneyKan(int money)
    {
        var kan = money / RecruitConstants.MoneyPerKan;
        return $"{kan:N0}贯";
    }
}
