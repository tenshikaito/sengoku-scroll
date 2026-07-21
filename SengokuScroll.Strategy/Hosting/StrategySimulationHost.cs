using SengokuScroll.Common.Types;
using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Persistence;
using SengokuScroll.Strategy.Rules;
using System.Text.Json;
using System.Text.Json.Serialization;
using SengokuScroll.Strategy.Systems;
using SengokuScroll.Strategy.Time;
using SengokuScroll.Strategy.Vision;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Hosting;

/// <summary>
/// 策略模式单机仿真宿主：加载剧本、下达移动、日推进。
/// 变更命令成功后返回完整 <see cref="StrategyWorldStateDto"/>（M2-a）。
/// </summary>
public sealed class StrategySimulationHost : IDisposable
{
    private StrategySimulationScope? simulation;
    private readonly StrategyTimeController timeController = new();
    private readonly object sync = new();

    /// <summary>当前已加载的剧本 Id；未加载时为 null。</summary>
    public string? LoadedScenarioId { get; private set; }

    /// <summary>是否已成功加载剧本。</summary>
    public bool IsLoaded => simulation is not null;

    /// <summary>从 Maps 目录加载 JSON 剧本并初始化仿真。</summary>
    public GameResult<StrategyWorldStateDto> LoadScenario(
        string scenarioId,
        StrategyLoadOptions? loadOptions = null)
    {
        lock (sync)
        {
            var path = ResolveScenarioPath(scenarioId);
            if (path is null)
                return GameError.DataNotFound;

            simulation?.Dispose();
            var loaded = StrategyScenarioLoader.LoadFromFile(path);
            var meta = StrategyScenarioLoader.ApplyLoadOptions(loaded.Meta, loadOptions);
            simulation = StrategySimulationBootstrap.CreateScope(loaded.World, meta);
            simulation.MovementTrace.Clear();
            simulation.Services.GetRequiredService<StrategyAiDecisionTrace>().Clear();
            LoadedScenarioId = scenarioId;
            timeController.Pause();

            RunMonthStartOnLoadIfNeeded(simulation);

            return BuildStateResult();
        }
    }

    /// <summary>合并两支友军：来源部队子编制并入目标部队后移除来源。</summary>
    public GameResult<StrategyWorldStateDto> OrderUnitMerge(int sourceUnitId, int targetUnitId)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var gameData = simulation.World.GameData;
            if (!gameData.Units.TryGetValue(sourceUnitId, out var source))
                return GameError.UnitError.UnitNotFound;

            if (!gameData.Units.TryGetValue(targetUnitId, out var target))
                return GameError.UnitError.UnitNotFound;

            var result = UnitMergeActions.MergeUnits(
                simulation.GameContext.GameWorldContext,
                source,
                target,
                gameData);
            if (!result.IsSuccess)
                return result.Error!;

            simulation.MovementTrace.Log(
                "UnitMerge",
                "部队合并",
                targetUnitId,
                target.Location,
                source.Location,
                $"source={sourceUnitId} soldiers={target.Soldier}");

            return BuildStateResult();
        }
    }

    /// <summary>从部队拆出子编制并在邻格生成新部队。</summary>
    public GameResult<StrategyWorldStateDto> OrderUnitSplit(
        int unitId,
        IReadOnlyList<int> subUnitIds,
        Point2 spawn,
        string? unitName = null)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var gameData = simulation.World.GameData;
            if (!gameData.Units.TryGetValue(unitId, out var parent))
                return GameError.UnitError.UnitNotFound;

            var spawnLocation = new Point3(spawn.X, spawn.Y);
            var result = UnitSplitActions.SplitSubUnits(
                simulation.GameContext.GameWorldContext,
                parent,
                subUnitIds,
                spawnLocation,
                gameData,
                unitName);
            if (!result.IsSuccess)
                return result.Error!;

            simulation.MovementTrace.Log(
                "UnitSplit",
                "部队分兵",
                unitId,
                parent.Location,
                spawnLocation,
                $"newUnit={result.Value!.Id} subUnits={subUnitIds.Count}");

            return BuildStateResult();
        }
    }

    /// <summary>从当主居城出征：扣减城内兵并在据点格生成部队。</summary>
    public GameResult<StrategyWorldStateDto> DeployFromStronghold(
        int strongholdId,
        string unitName,
        int commanderId,
        IReadOnlyList<StrategyDeployCompositionEntry> composition,
        int? food = null,
        int? money = null)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var meta = simulation.ScenarioMeta;
            var gameData = simulation.World.GameData;
            if (!gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            var result = UnitDeploymentActions.DeployFromStronghold(
                simulation.GameContext.GameWorldContext,
                stronghold,
                meta,
                gameData,
                meta.PlayerForceId,
                unitName,
                commanderId,
                composition,
                food,
                money);
            if (!result.IsSuccess)
                return result.Error!;

            simulation.MovementTrace.Log(
                "StrongholdDeploy",
                "居城出征",
                result.Value!.Id,
                stronghold.Location,
                stronghold.Location,
                $"stronghold={strongholdId} commander={commanderId} soldiers={result.Value.Soldier}");

            return BuildStateResult();
        }
    }

    /// <summary>为军事单位寻路并进入移动状态（可经中继格拼接路径）。</summary>
    public GameResult<StrategyWorldStateDto> OrderUnitMove(
        int unitId,
        Point2 target,
        IReadOnlyList<Point2>? via = null)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            if (!simulation.World.GameData.Units.TryGetValue(unitId, out var unit))
                return GameError.UnitError.UnitNotFound;

            if (SiegeOrderRules.IsSiegeMovementLocked(unit))
                return GameError.MovementError.CannotMoveToTile;

            var pathfinding = simulation.Services.GetRequiredService<IPathfindingService>();
            var start = (Point2)unit.Location;
            var stops = BuildStopList(start, target, via);
            var path = BuildPathThrough(pathfinding, unit, stops, start);
            if (!path.IsSuccess)
                return path.Error!;

            unit.Status = UnitStatus.Moving;
            unit.ActionTarget.RoutePoints.Clear();

            foreach (var node in path.Value!.Skip(1))
                unit.ActionTarget.RoutePoints.Enqueue(node.Location);

            var routeText = string.Join(" -> ", unit.ActionTarget.RoutePoints.Select(p => p.ToString()));
            simulation.MovementTrace.Log(
                "OrderMove",
                "下达移动",
                unitId,
                unit.Location,
                target,
                $"route=[{routeText}] via={via?.Count ?? 0} status={unit.Status} AP={unit.Ap}");

            return BuildStateResult();
        }
    }

    /// <summary>变更单位方针；玩家势力从当主所在格下达，异格经信使（M3-b）。</summary>
    public GameResult<StrategyPolicyChangeResponseDto> OrderUnitDirective(
        int unitId,
        UnitDirective directive)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            if (!simulation.World.GameData.Units.TryGetValue(unitId, out var unit))
                return GameError.UnitError.UnitNotFound;

            var meta = simulation.ScenarioMeta;
            var gameData = simulation.World.GameData;
            var helper = simulation.Services.GetRequiredService<MessengerDispatchHelper>();
            var issuer = StrategyLordHelper.ResolvePolicyIssuerLocation(unit, gameData, meta);
            var strongholdId = StrategyLordHelper.ResolveSourceStrongholdId(gameData, meta, issuer);

            var outcome = helper.IssuePolicyChange(issuer, strongholdId, unit, directive);

            simulation.MovementTrace.Log(
                "PolicyChange",
                outcome == MessengerDispatchOutcome.AppliedImmediately ? "方针即时生效" : "方针信使派出",
                unitId,
                issuer,
                unit.Location,
                $"directive={directive} outcome={outcome} lord=({issuer.X},{issuer.Y}) stronghold={strongholdId}");

            var world = BuildStateResult();
            if (!world.IsSuccess)
                return world.Error!;

            return new StrategyPolicyChangeResponseDto
            {
                State = world.Value!,
                Outcome = outcome.ToString()
            };
        }
    }

    /// <summary>下达攻击命令（相邻敌军）；日推进后由系统结算（M3-b）。</summary>
    public GameResult<StrategyWorldStateDto> OrderUnitAttack(int unitId, Point2 target)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var battle = simulation.Services.GetRequiredService<StrategyInstantBattleSystem>();
            var preview = battle.Preview(unitId, target);
            if (!preview.IsSuccess)
                return preview.Error!;

            if (!simulation.World.GameData.Units.TryGetValue(unitId, out var unit))
                return GameError.UnitError.UnitNotFound;

            UnitBattleActions.QueueAttack(unit, preview.Value!.DefenderUnitId);

            simulation.MovementTrace.Log(
                "AttackOrder",
                "攻击命令已下达，待日推进结算",
                unitId,
                unit.Location,
                target,
                $"defender={preview.Value.DefenderUnitId} attAp={unit.Ap}");

            return BuildStateResult();
        }
    }

    /// <summary>对敌方据点下达攻城指令（强攻 / 包围，消耗 AP）。</summary>
    public GameResult<StrategyWorldStateDto> OrderUnitSiege(int unitId, int strongholdId, UnitSiegeMode mode)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            if (!simulation.World.GameData.Units.TryGetValue(unitId, out var unit))
                return GameError.UnitError.UnitNotFound;

            if (!simulation.World.GameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
                return GameError.DataNotFound;

            var rules = simulation.Services.GetRequiredService<GameRuleConfig>();
            var validate = SiegeOrderRules.Validate(
                unit, stronghold, mode, simulation.World.GameData, rules.SiegeOrderAp);
            if (!validate.IsSuccess)
                return validate.Error!;

            SiegeOrderRules.Apply(
                simulation.GameContext.GameWorldContext,
                unit,
                stronghold,
                mode,
                simulation.World.GameData,
                rules.SiegeOrderAp,
                simulation.ScenarioMeta);

            var battleReportDelivery = simulation.Services.GetRequiredService<BattleReportDeliveryHelper>();
            var siegeDefender = StrongholdGarrisonRules.FindGarrisonUnit(stronghold, simulation.World.GameData);
            battleReportDelivery.DeliverSiegeOrderStartedReport(
                unit,
                stronghold,
                mode,
                simulation.World.GameData,
                siegeDefender);

            if (SiegeOrderRules.CanCaptureViaAssaultOrder(unit, stronghold, simulation.World.GameData))
            {
                // 业务：强攻后守军溃灭则即时占领据点
                var captureHelper = simulation.Services.GetRequiredService<StrongholdCaptureHelper>();
                captureHelper.CaptureStronghold(unit, stronghold, stronghold.ForceId, simulation.World.GameData);
            }

            simulation.MovementTrace.Log(
                "SiegeOrder",
                $"攻城指令 {mode}",
                unitId,
                unit.Location,
                stronghold.Location,
                $"stronghold={strongholdId} ap_left={unit.Ap}");

            return BuildStateResult();
        }
    }

    /// <summary>预览寻路（可选起点与中继，不修改仿真状态）。</summary>
    public GameResult<StrategyPathPreviewDto> PreviewUnitPath(
        int unitId,
        Point2 target,
        Point2? from = null,
        IReadOnlyList<Point2>? via = null)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            if (!simulation.World.GameData.Units.TryGetValue(unitId, out var unit))
                return GameError.UnitError.UnitNotFound;

            var pathfinding = simulation.Services.GetRequiredService<IPathfindingService>();
            var movementRules = simulation.Services.GetRequiredService<Domain.Rules.MovementRules>();
            var visibilityLedger = simulation.Services.GetRequiredService<StrategyVisibilityLedger>();
            visibilityLedger.Recompute(simulation.World, simulation.ScenarioMeta);
            var visibility = visibilityLedger.GetOrCreate(simulation.ScenarioMeta.PlayerForceId);
            var pathBlockCheck = StrategyPreviewPathRules.BuildFogAwarePathBlockCheck(
                movementRules,
                unit,
                simulation.World.GameData,
                simulation.ScenarioMeta,
                visibility);

            var start = from ?? (Point2)unit.Location;
            var stops = BuildStopList(start, target, via);
            var path = BuildPathThrough(pathfinding, unit, stops, start, pathBlockCheck);
            if (!path.IsSuccess)
                return path.Error!;

            var pathPoints = path.Value!
                .Select(node => new StrategyMapPointDto { X = node.Location.X, Y = node.Location.Y })
                .ToList();

            simulation.MovementTrace.Log(
                "PreviewPath",
                "path preview",
                unitId,
                start,
                target,
                $"fromParam={(from.HasValue ? from.Value.ToString() : "null")} " +
                $"unitLoc={unit.Location} stops={stops.Count} " +
                $"first={pathPoints.FirstOrDefault()} last={pathPoints.LastOrDefault()} count={pathPoints.Count}");

            return new StrategyPathPreviewDto
            {
                Points = pathPoints
            };
        }
    }

    private static List<Point2> BuildStopList(Point2 start, Point2 target, IReadOnlyList<Point2>? via)
    {
        var stops = new List<Point2>();
        if (via is not null)
            stops.AddRange(via);

        if (stops.Count == 0 || stops[^1] != target)
            stops.Add(target);

        if (stops.Count > 0 && stops[0] == start)
            stops.RemoveAt(0);

        return stops;
    }

    private static GameResult<List<PathNode>> BuildPathThrough(
        IPathfindingService pathfinding,
        Domain.Entities.Unit unit,
        IReadOnlyList<Point2> stops,
        Point2 pathStart,
        Func<Point2, bool>? isPathTileBlocked = null)
    {
        if (stops.Count == 0)
            return GameError.MovementError.CannotMoveToTile;

        var merged = new List<PathNode>();
        var segmentStart = pathStart;

        foreach (var stop in stops)
        {
            var segment = pathfinding.CalculatePathFrom(segmentStart, stop, unit, isPathTileBlocked);
            if (segment is null || segment.Count <= 1)
                return GameError.MovementError.CannotMoveToTile;

            if (merged.Count == 0)
                merged.AddRange(segment);
            else
                merged.AddRange(segment.Skip(1));

            segmentStart = stop;
        }

        return merged;
    }

    /// <summary>预览对相邻格敌军的瞬间战（不修改状态）。</summary>
    public GameResult<StrategyBattlePreviewDto> PreviewUnitAttack(int unitId, Point2 target)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var battle = simulation.Services.GetRequiredService<StrategyInstantBattleSystem>();
            return battle.Preview(unitId, target);
        }
    }

    /// <summary>对相邻格敌军执行瞬间战并返回世界状态与战斗结果。</summary>
    public GameResult<StrategyInstantBattleResponseDto> ExecuteInstantBattle(int unitId, Point2 target)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var battle = simulation.Services.GetRequiredService<StrategyInstantBattleSystem>();
            var result = battle.Execute(unitId, target);
            if (!result)
                return result.Error!;

            var (preview, outcome, tactical) = result.Value!;
            simulation.MovementTrace.Log(
                "InstantBattle",
                outcome.AttackerWon ? "攻方胜" : "守方胜",
                unitId,
                detail:
                    $"vs={preview.DefenderUnitId} seed={outcome.ResolutionSeed} roll={outcome.ResolutionRoll} attLoss={outcome.AttackerCasualties} defLoss={outcome.DefenderCasualties}");

            var world = BuildStateResult();
            if (!world.IsSuccess)
                return world.Error!;

            var attackerUnit = simulation!.World.GameData.Units[unitId];
            var defenderUnit = simulation.World.GameData.Units[preview.DefenderUnitId];

            var engagementKind = BattleEngagementClassifier.Classify(attackerUnit, defenderUnit, simulation.World.GameData);

            return new StrategyInstantBattleResponseDto
            {
                State = world.Value!,
                Result = new StrategyBattleResultDto
                {
                    AttackerWon = outcome.AttackerWon,
                    AttackerUnitId = unitId,
                    DefenderUnitId = preview.DefenderUnitId,
                    AttackerForceId = attackerUnit.ForceId,
                    DefenderForceId = defenderUnit.ForceId,
                    AttackerName = attackerUnit.Name,
                    DefenderName = defenderUnit.Name,
                    AttackerSoldiersBefore = outcome.AttackerSoldiersBefore,
                    DefenderSoldiersBefore = outcome.DefenderSoldiersBefore,
                    AttackerCasualties = outcome.AttackerCasualties,
                    DefenderCasualties = outcome.DefenderCasualties,
                    AttackerSoldiersAfter = attackerUnit.Soldier,
                    DefenderSoldiersAfter = defenderUnit.Soldier,
                    AttackerWinRatePercent = outcome.AttackerWinRatePercent,
                    ResolutionSeed = outcome.ResolutionSeed,
                    ResolutionRoll = outcome.ResolutionRoll,
                    EngagementKind = engagementKind.ToString(),
                    LogEntries = tactical.LogEntries,
                    FactorNotes = []
                }
            };
        }
    }

    /// <summary>推进 1 天并执行策略系统链。</summary>
    public GameResult<StrategyAdvanceDayResponseDto> AdvanceDay()
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var dayOutcomeBuffer = simulation.Services.GetRequiredService<StrategyDayOutcomeBuffer>();
            var dayDebugLog = simulation.Services.GetRequiredService<IStrategyDayDebugLog>();
            dayOutcomeBuffer.Clear();

            var upcoming = simulation.World.GameData.GameDate.AddDays(1);
            dayDebugLog.BeginDay(upcoming.Year, upcoming.Month, upcoming.Day, LoadedScenarioId);

            timeController.AdvanceDay(simulation.World, simulation.Engine);

            dayDebugLog.EndDay(dayOutcomeBuffer.ResolvedBattles.Count, dayOutcomeBuffer.Events.Count);
            simulation.MovementTrace.Log("AdvanceDay", "日推进完成", detail:
                $"{simulation.World.GameData.GameDate.Year}-{simulation.World.GameData.GameDate.Month}-{simulation.World.GameData.GameDate.Day} battles={dayOutcomeBuffer.ResolvedBattles.Count}");

            var world = BuildStateResult();
            if (!world.IsSuccess)
                return world.Error!;

            return new StrategyAdvanceDayResponseDto
            {
                State = world.Value!,
                ResolvedBattles = [.. dayOutcomeBuffer.ResolvedBattles],
                Events = [.. dayOutcomeBuffer.Events],
                DayDebugLogPath = dayDebugLog.LastWrittenFilePath,
                DayDebugEntryCount = dayDebugLog.Snapshot().Count
            };
        }
    }

    /// <summary>获取当前世界快照（供 API 返回）。</summary>
    public GameResult<StrategyWorldStateDto> GetState()
    {
        lock (sync)
            return BuildStateResult();
    }

    /// <summary>获取当前剧本地图静态主数据（地形/区域/道路/地标）。</summary>
    public GameResult<StrategyMapMasterDto> GetMapMaster()
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            return StrategyWorldStateMapper.ToMapMasterDto(
                simulation.World,
                LoadedScenarioId ?? string.Empty);
        }
    }

    /// <summary>捕获当前仿真为 JSON 存档。</summary>
    public GameResult<StrategySaveDocument> CaptureSave()
    {
        lock (sync)
        {
            if (simulation is null || LoadedScenarioId is null)
                return GameError.DataNotFound;

            return StrategyWorldSaveService.Capture(
                simulation.World,
                LoadedScenarioId,
                simulation.ScenarioMeta.PlayerForceId,
                simulation.Services.GetRequiredService<StrategyVisibilityLedger>());
        }
    }

    /// <summary>从存档恢复：先加载剧本再覆盖可变状态。</summary>
    public GameResult<StrategyWorldStateDto> RestoreSave(StrategySaveDocument save)
    {
        lock (sync)
        {
            if (string.IsNullOrWhiteSpace(save.ScenarioId))
                return GameError.DataNotFound;

            var loadResult = LoadScenario(save.ScenarioId);
            if (!loadResult.IsSuccess)
                return loadResult.Error!;

            if (simulation is null)
                return GameError.DataNotFound;

            StrategyWorldSaveService.Apply(save, simulation.World);
            simulation.MovementTrace.Clear();
            simulation.Services.GetRequiredService<StrategyAiDecisionTrace>().Clear();

            var ledger = simulation.Services.GetRequiredService<StrategyVisibilityLedger>();
            if (save.Visibility is not null)
            {
                var tileMap = simulation.World.GameMapMasterData.TileMap;
                ledger.ApplySave(
                    save.PlayerForceId,
                    save.Visibility,
                    tileMap.Width,
                    tileMap.Height);
            }

            ledger.Recompute(simulation.World, simulation.ScenarioMeta);

            return BuildStateResult();
        }
    }

    private static readonly JsonSerializerOptions SaveJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    /// <summary>序列化存档为 JSON 字符串。</summary>
    public static string SerializeSave(StrategySaveDocument save)
        => JsonSerializer.Serialize(save, SaveJsonOptions);

    /// <summary>从 JSON 字符串反序列化存档。</summary>
    public static GameResult<StrategySaveDocument> DeserializeSave(string json)
    {
        try
        {
            var save = JsonSerializer.Deserialize<StrategySaveDocument>(json, SaveJsonOptions);
            return save is null ? GameError.DataNotFound : save;
        }
        catch (JsonException)
        {
            return GameError.DataNotFound;
        }
    }

    /// <summary>获取移动诊断追踪（最近 200 条）。</summary>
    public IReadOnlyList<StrategyMovementTraceEntry> GetMovementTrace()
    {
        lock (sync)
            return simulation?.MovementTrace.Snapshot() ?? [];
    }

    /// <summary>获取 AI 决策思维链追踪（最近 400 条）。</summary>
    public IReadOnlyList<StrategyAiDecisionTraceEntry> GetAiDecisionTrace()
    {
        lock (sync)
        {
            if (simulation is null)
                return [];

            return simulation.Services.GetRequiredService<StrategyAiDecisionTrace>().Snapshot();
        }
    }

    /// <summary>获取日推进 debug 日志快照（内存缓冲 + 最近文件路径）。</summary>
    public StrategyDayDebugLogSnapshotDto GetDayDebugLog()
    {
        lock (sync)
        {
            if (simulation is null)
            {
                return new StrategyDayDebugLogSnapshotDto
                {
                    Enabled = false,
                    LastWrittenFilePath = null,
                    Entries = []
                };
            }

            var log = simulation.Services.GetRequiredService<IStrategyDayDebugLog>();
            return new StrategyDayDebugLogSnapshotDto
            {
                Enabled = log.IsEnabled,
                LastWrittenFilePath = log.LastWrittenFilePath,
                Entries = log.Snapshot().Select(e => new StrategyDayDebugEntryDto
                {
                    Sequence = e.Sequence,
                    At = e.At.ToString("O"),
                    GameYear = e.GameYear,
                    GameMonth = e.GameMonth,
                    GameDay = e.GameDay,
                    Category = e.Category,
                    Message = e.Message
                }).ToList()
            };
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (sync)
        {
            simulation?.Dispose();
            simulation = null;
            LoadedScenarioId = null;
        }
    }

    private static void RunMonthStartOnLoadIfNeeded(StrategySimulationScope simulation)
    {
        // 业务：加载日恰为月初结算日时，补发当月领主贡纳
        if (!EconomyRules.IsMonthlySettlementDay(simulation.World.GameData.GameDate))
            return;

        simulation.Services.GetRequiredService<SupplyConvoyDispatchHelper>()
            .DispatchMonthlyLordTributes();
    }

    /// <summary>登记谍报成果（开发/任务用；约 2 个月后过期）。</summary>
    public GameResult<StrategyWorldStateDto> RecordEspionageIntel(
        string targetKind,
        int targetId,
        string scope,
        string precision)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            if (!Enum.TryParse<EspionageIntelTargetKind>(targetKind, ignoreCase: true, out var kind))
                return GameError.DataNotFound;

            if (!Enum.TryParse<EspionageIntelScope>(scope, ignoreCase: true, out var scopeEnum))
                return GameError.DataNotFound;

            if (!Enum.TryParse<EspionageIntelPrecision>(precision, ignoreCase: true, out var precisionEnum))
                return GameError.DataNotFound;

            var ledger = simulation.Services.GetRequiredService<StrategyEspionageIntelLedger>();
            ledger.RecordMission(
                simulation.ScenarioMeta.PlayerForceId,
                kind,
                targetId,
                scopeEnum,
                precisionEnum,
                simulation.World.GameData.GameDate);

            return BuildStateResult();
        }
    }

    private GameResult<StrategyWorldStateDto> BuildStateResult()
    {
        if (simulation is null)
            return GameError.DataNotFound;

        simulation.Services.GetRequiredService<StrategyVisibilityLedger>()
            .Recompute(simulation.World, simulation.ScenarioMeta);

        return StrategyWorldStateMapper.ToDto(
            simulation.World,
            LoadedScenarioId ?? string.Empty,
            simulation.ScenarioMeta,
            simulation.Services.GetRequiredService<StrategyVisibilityLedger>(),
            simulation.Services.GetRequiredService<StrategyEspionageIntelLedger>());
    }

    private static string? ResolveScenarioPath(string scenarioId)
    {
        var fileName = scenarioId.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? scenarioId
            : $"{scenarioId}.json";

        foreach (var directory in GetMapSearchDirectories())
        {
            var path = Path.Combine(directory, fileName);
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private static IEnumerable<string> GetMapSearchDirectories()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "Maps");

        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 6 && !string.IsNullOrEmpty(dir); i++)
        {
            var candidate = Path.Combine(dir, "SengokuScroll.Strategy", "Maps");
            if (Directory.Exists(candidate))
                yield return candidate;

            dir = Directory.GetParent(dir)?.FullName;
        }
    }
}
