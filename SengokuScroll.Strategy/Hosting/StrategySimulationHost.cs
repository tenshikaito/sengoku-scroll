using SengokuScroll.Common.Types;
using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Strategy.Diagnostics;
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
    public GameResult<StrategyWorldStateDto> LoadScenario(string scenarioId)
    {
        lock (sync)
        {
            var path = ResolveScenarioPath(scenarioId);
            if (path is null)
                return GameError.DataNotFound;

            simulation?.Dispose();
            var loaded = StrategyScenarioLoader.LoadFromFile(path);
            simulation = StrategySimulationBootstrap.CreateScope(loaded.World, loaded.Meta);
            simulation.MovementTrace.Clear();
            LoadedScenarioId = scenarioId;
            timeController.Pause();

            RunMonthStartOnLoadIfNeeded(simulation);

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
            var start = from ?? (Point2)unit.Location;
            var stops = BuildStopList(start, target, via);
            var path = BuildPathThrough(pathfinding, unit, stops, start);
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
        Point2 pathStart)
    {
        if (stops.Count == 0)
            return GameError.MovementError.CannotMoveToTile;

        var merged = new List<PathNode>();
        var segmentStart = pathStart;

        foreach (var stop in stops)
        {
            var segment = pathfinding.CalculatePathFrom(segmentStart, stop, unit);
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

            var (preview, outcome) = result.Value!;
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

            return new StrategyInstantBattleResponseDto
            {
                State = world.Value!,
                Result = new StrategyBattleResultDto
                {
                    AttackerWon = outcome.AttackerWon,
                    AttackerUnitId = unitId,
                    DefenderUnitId = preview.DefenderUnitId,
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
                    LogEntries = InstantBattleCalculator.BuildBattleLog(attackerUnit, defenderUnit, outcome)
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
            dayOutcomeBuffer.Clear();

            timeController.AdvanceDay(simulation.World, simulation.Engine);
            simulation.MovementTrace.Log("AdvanceDay", "日推进完成", detail:
                $"{simulation.World.GameData.GameDate.Year}-{simulation.World.GameData.GameDate.Month}-{simulation.World.GameData.GameDate.Day} battles={dayOutcomeBuffer.ResolvedBattles.Count}");

            var world = BuildStateResult();
            if (!world.IsSuccess)
                return world.Error!;

            return new StrategyAdvanceDayResponseDto
            {
                State = world.Value!,
                ResolvedBattles = dayOutcomeBuffer.ResolvedBattles.ToList(),
                Events = dayOutcomeBuffer.Events.ToList()
            };
        }
    }

    /// <summary>获取当前世界快照（供 API 返回）。</summary>
    public GameResult<StrategyWorldStateDto> GetState()
    {
        lock (sync)
            return BuildStateResult();
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
                simulation.ScenarioMeta.PlayerForceId);
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
        if (!EconomyRules.IsMonthlySettlementDay(simulation.World.GameData.GameDate))
            return;

        simulation.Services.GetRequiredService<SupplyConvoyDispatchHelper>()
            .DispatchMonthlyLordTributes();
    }

    private GameResult<StrategyWorldStateDto> BuildStateResult()
    {
        if (simulation is null)
            return GameError.DataNotFound;

        return StrategyWorldStateMapper.ToDto(
            simulation.World,
            LoadedScenarioId ?? string.Empty,
            simulation.ScenarioMeta);
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
