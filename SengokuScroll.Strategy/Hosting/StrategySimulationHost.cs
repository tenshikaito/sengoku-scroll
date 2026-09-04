using SengokuScroll.Common.Types;
using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
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

    /// <summary>当前玩家当主名（存档摘要用）。</summary>
    public string? LordName
    {
        get
        {
            lock (sync)
                return simulation?.ScenarioMeta.LordName;
        }
    }

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
            StrongholdCityActorBootstrapHelper.EnsureCityActors(
                simulation.World.GameData,
                simulation.Services.GetRequiredService<StrategyForceLordRegistry>());
            StrategyAiBootstrapHelper.BootstrapAggressiveDirectives(simulation.World, meta);
            IntelEntityBootstrapHelper.BootstrapGameWorld(simulation.World, meta);
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

            var ownership = PlayerUnitControlRules.ValidateOwnership(source, simulation.ScenarioMeta);
            if (!ownership.IsSuccess)
                return ownership.Error!;

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

            var ownership = PlayerUnitControlRules.ValidateOwnership(parent, simulation.ScenarioMeta);
            if (!ownership.IsSuccess)
                return ownership.Error!;

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

    /// <summary>从当主居城组建部队；默认在城中，可选出城。</summary>
    public GameResult<StrategyWorldStateDto> DeployFromStronghold(
        int strongholdId,
        string unitName,
        int commanderId,
        IReadOnlyList<StrategyDeployCompositionEntry> composition,
        int? food = null,
        int? money = null,
        bool deployToMap = false)
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
                money,
                deployToMap);
            if (!result.IsSuccess)
                return result.Error!;

            simulation.MovementTrace.Log(
                "StrongholdDeploy",
                deployToMap ? "居城出城" : "居城组建",
                result.Value!.Id,
                stronghold.Location,
                stronghold.Location,
                $"stronghold={strongholdId} commander={commanderId} soldiers={result.Value.Soldier} inStronghold={result.Value.InStronghold}");

            return BuildStateResult();
        }
    }

    /// <summary>单位入城（InStronghold）。</summary>
    public GameResult<StrategyWorldStateDto> EnterUnitStronghold(int unitId, int strongholdId)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var gameData = simulation.World.GameData;
            if (!gameData.Units.TryGetValue(unitId, out var unit))
                return GameError.UnitError.UnitNotFound;
            var ownership = PlayerUnitControlRules.ValidateOwnership(unit, simulation.ScenarioMeta);
            if (!ownership.IsSuccess)
                return ownership.Error!;
            if (!gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            var result = UnitStrongholdPresenceActions.EnterStronghold(
                simulation.GameContext.GameWorldContext,
                unit,
                stronghold,
                gameData,
                simulation.ScenarioMeta);
            return result.IsSuccess ? BuildStateResult() : result.Error!;
        }
    }

    /// <summary>单位出城。</summary>
    public GameResult<StrategyWorldStateDto> ExitUnitStronghold(int unitId, int strongholdId)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var gameData = simulation.World.GameData;
            if (!gameData.Units.TryGetValue(unitId, out var unit))
                return GameError.UnitError.UnitNotFound;
            var ownership = PlayerUnitControlRules.ValidateOwnership(unit, simulation.ScenarioMeta);
            if (!ownership.IsSuccess)
                return ownership.Error!;
            if (!gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            var result = UnitStrongholdPresenceActions.ExitStronghold(
                simulation.GameContext.GameWorldContext,
                unit,
                stronghold,
                gameData);
            return result.IsSuccess ? BuildStateResult() : result.Error!;
        }
    }

    /// <summary>建制解散（仅 Home 据点）。</summary>
    public GameResult<StrategyWorldStateDto> DisbandUnitOrganizationally(int unitId)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var gameData = simulation.World.GameData;
            if (!gameData.Units.TryGetValue(unitId, out var unit))
                return GameError.UnitError.UnitNotFound;

            var ownership = PlayerUnitControlRules.ValidateOwnership(unit, simulation.ScenarioMeta);
            if (!ownership.IsSuccess)
                return ownership.Error!;

            var result = UnitStrongholdPresenceActions.OrganizationalDisband(
                simulation.GameContext.GameWorldContext,
                unit,
                gameData);
            return result.IsSuccess ? BuildStateResult() : result.Error!;
        }
    }

    /// <summary>创立商店（无需许可；商业值≥20，每商人组织每城 1 店）。</summary>
    public GameResult<StrategyWorldStateDto> CreateMerchantShop(int strongholdId, string? houseName = null)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var gameData = simulation.World.GameData;
            if (!gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            var result = StrongholdShopActions.CreateShop(
                gameData,
                stronghold,
                simulation.ScenarioMeta.PlayerForceId,
                houseName);
            return result.IsSuccess ? BuildStateResult() : result.Error!;
        }
    }

    /// <summary>城内 Unit 市价买入粮食（砸单）。</summary>
    public GameResult<StrategyWorldStateDto> UnitSmashBuyFood(
        int unitId,
        int maxPriceMoneyPerGo,
        int quantityGo = 0)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var gameData = simulation.World.GameData;
            if (!gameData.Units.TryGetValue(unitId, out var unit))
                return GameError.UnitError.UnitNotFound;

            var ownership = PlayerUnitControlRules.ValidateOwnership(unit, simulation.ScenarioMeta);
            if (!ownership.IsSuccess)
                return ownership.Error!;

            if (!gameData.Strongholds.TryGetValue(unit.LocationStrongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            if (!UnitTradeActions.CanTradeAtStronghold(unit, stronghold, gameData))
                return GameError.MarketError.TradeNotAllowed;

            var ledger = simulation.Services.GetRequiredService<MerchantTaxLedger>();
            var bought = UnitTradeActions.SmashBuyFood(
                unit, stronghold, gameData, ledger, maxPriceMoneyPerGo, quantityGo);
            return bought > 0 ? BuildStateResult() : GameError.MarketError.TradeNotFilled;
        }
    }

    /// <summary>城内 Unit 市价卖出粮食（砸单）。</summary>
    public GameResult<StrategyWorldStateDto> UnitSmashSellFood(
        int unitId,
        int minPriceMoneyPerGo,
        int quantityGo = 0)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var gameData = simulation.World.GameData;
            if (!gameData.Units.TryGetValue(unitId, out var unit))
                return GameError.UnitError.UnitNotFound;

            var ownership = PlayerUnitControlRules.ValidateOwnership(unit, simulation.ScenarioMeta);
            if (!ownership.IsSuccess)
                return ownership.Error!;

            if (!gameData.Strongholds.TryGetValue(unit.LocationStrongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            if (!UnitTradeActions.CanTradeAtStronghold(unit, stronghold, gameData))
                return GameError.MarketError.TradeNotAllowed;

            var ledger = simulation.Services.GetRequiredService<MerchantTaxLedger>();
            var sold = UnitTradeActions.SmashSellFood(
                unit, stronghold, gameData, ledger, minPriceMoneyPerGo, quantityGo);
            return sold > 0 ? BuildStateResult() : GameError.MarketError.TradeNotFilled;
        }
    }

    /// <summary>据点市场快照（市场窗口 UI）。</summary>
    public GameResult<StrategyMarketSnapshotDto> GetMarketSnapshot(
        int strongholdId,
        MarketCommodityType commodity = MarketCommodityType.Food)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var gameData = simulation.World.GameData;
            if (!gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            var playerForceId = simulation.ScenarioMeta.PlayerForceId;
            return MarketSnapshotHelper.BuildSnapshot(stronghold, gameData, commodity, playerForceId: playerForceId);
        }
    }

    /// <summary>当主撤销官府挂单。</summary>
    public GameResult<StrategyWorldStateDto> StrongholdLordCancelMarketOrder(
        int strongholdId,
        int orderId,
        MarketCommodityType commodity = MarketCommodityType.Food)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var gameData = simulation.World.GameData;
            var meta = simulation.ScenarioMeta;
            if (!gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            if (!StrongholdLordTradeActions.CancelMarketOrder(
                    stronghold,
                    meta.PlayerForceId,
                    meta,
                    gameData,
                    orderId,
                    commodity,
                    out var error))
            {
                return error ?? GameError.MarketError.OrderNotFound;
            }

            return BuildStateResult();
        }
    }

    /// <summary>当主以官府库在市价买入粮食。</summary>
    public GameResult<StrategyWorldStateDto> StrongholdLordSmashBuyFood(
        int strongholdId,
        int maxPriceMoneyPerGo,
        int quantityGo = 0)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var gameData = simulation.World.GameData;
            var meta = simulation.ScenarioMeta;
            if (!gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            if (!StrongholdLordTradeActions.CanLordTradeAtStronghold(
                    stronghold,
                    meta.PlayerForceId,
                    meta,
                    gameData))
            {
                return GameError.MarketError.TradeNotAllowed;
            }

            var ledger = simulation.Services.GetRequiredService<MerchantTaxLedger>();
            var result = StrongholdLordTradeActions.LimitBuyFood(
                stronghold,
                meta.PlayerForceId,
                meta,
                gameData,
                ledger,
                maxPriceMoneyPerGo,
                quantityGo);
            return result.HasTradeEffect ? BuildStateResult() : GameError.MarketError.TradeNotFilled;
        }
    }

    /// <summary>当主以官府库在市价卖出粮食。</summary>
    public GameResult<StrategyWorldStateDto> StrongholdLordSmashSellFood(
        int strongholdId,
        int minPriceMoneyPerGo,
        int quantityGo = 0)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var gameData = simulation.World.GameData;
            var meta = simulation.ScenarioMeta;
            if (!gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            if (!StrongholdLordTradeActions.CanLordTradeAtStronghold(
                    stronghold,
                    meta.PlayerForceId,
                    meta,
                    gameData))
            {
                return GameError.MarketError.TradeNotAllowed;
            }

            var ledger = simulation.Services.GetRequiredService<MerchantTaxLedger>();
            var result = StrongholdLordTradeActions.LimitSellFood(
                stronghold,
                meta.PlayerForceId,
                meta,
                gameData,
                ledger,
                minPriceMoneyPerGo,
                quantityGo);
            return result.HasTradeEffect ? BuildStateResult() : GameError.MarketError.TradeNotFilled;
        }
    }

    /// <summary>当主以官府库在市价买入马匹。</summary>
    public GameResult<StrategyWorldStateDto> StrongholdLordSmashBuyHorse(
        int strongholdId,
        int maxPriceMoneyPerUnit,
        int quantity = 0)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var gameData = simulation.World.GameData;
            var meta = simulation.ScenarioMeta;
            if (!gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            if (!StrongholdLordTradeActions.CanLordTradeAtStronghold(
                    stronghold,
                    meta.PlayerForceId,
                    meta,
                    gameData))
            {
                return GameError.MarketError.TradeNotAllowed;
            }

            var ledger = simulation.Services.GetRequiredService<MerchantTaxLedger>();
            var result = StrongholdLordTradeActions.LimitBuyHorse(
                stronghold,
                meta.PlayerForceId,
                meta,
                gameData,
                ledger,
                maxPriceMoneyPerUnit,
                quantity);
            return result.HasTradeEffect ? BuildStateResult() : GameError.MarketError.TradeNotFilled;
        }
    }

    /// <summary>当主以官府库在市价卖出马匹。</summary>
    public GameResult<StrategyWorldStateDto> StrongholdLordSmashSellHorse(
        int strongholdId,
        int minPriceMoneyPerUnit,
        int quantity = 0)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var gameData = simulation.World.GameData;
            var meta = simulation.ScenarioMeta;
            if (!gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            if (!StrongholdLordTradeActions.CanLordTradeAtStronghold(
                    stronghold,
                    meta.PlayerForceId,
                    meta,
                    gameData))
            {
                return GameError.MarketError.TradeNotAllowed;
            }

            var ledger = simulation.Services.GetRequiredService<MerchantTaxLedger>();
            var result = StrongholdLordTradeActions.LimitSellHorse(
                stronghold,
                meta.PlayerForceId,
                meta,
                gameData,
                ledger,
                minPriceMoneyPerUnit,
                quantity);
            return result.HasTradeEffect ? BuildStateResult() : GameError.MarketError.TradeNotFilled;
        }
    }

    /// <summary>城内 Unit 市价买入马匹（砸单）。</summary>
    public GameResult<StrategyWorldStateDto> UnitSmashBuyHorse(
        int unitId,
        int maxPriceMoneyPerUnit,
        int quantity = 0)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var gameData = simulation.World.GameData;
            if (!gameData.Units.TryGetValue(unitId, out var unit))
                return GameError.UnitError.UnitNotFound;

            var ownership = PlayerUnitControlRules.ValidateOwnership(unit, simulation.ScenarioMeta);
            if (!ownership.IsSuccess)
                return ownership.Error!;

            if (!gameData.Strongholds.TryGetValue(unit.LocationStrongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            if (!UnitTradeActions.CanTradeAtStronghold(unit, stronghold, gameData))
                return GameError.MarketError.TradeNotAllowed;

            var ledger = simulation.Services.GetRequiredService<MerchantTaxLedger>();
            var bought = UnitTradeActions.SmashBuyHorse(
                unit, stronghold, gameData, ledger, maxPriceMoneyPerUnit, quantity);
            return bought > 0 ? BuildStateResult() : GameError.MarketError.TradeNotFilled;
        }
    }

    /// <summary>城内 Unit 市价卖出马匹（砸单）。</summary>
    public GameResult<StrategyWorldStateDto> UnitSmashSellHorse(
        int unitId,
        int minPriceMoneyPerUnit,
        int quantity = 0)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var gameData = simulation.World.GameData;
            if (!gameData.Units.TryGetValue(unitId, out var unit))
                return GameError.UnitError.UnitNotFound;

            var ownership = PlayerUnitControlRules.ValidateOwnership(unit, simulation.ScenarioMeta);
            if (!ownership.IsSuccess)
                return ownership.Error!;

            if (!gameData.Strongholds.TryGetValue(unit.LocationStrongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            if (!UnitTradeActions.CanTradeAtStronghold(unit, stronghold, gameData))
                return GameError.MarketError.TradeNotAllowed;

            var ledger = simulation.Services.GetRequiredService<MerchantTaxLedger>();
            var sold = UnitTradeActions.SmashSellHorse(
                unit, stronghold, gameData, ledger, minPriceMoneyPerUnit, quantity);
            return sold > 0 ? BuildStateResult() : GameError.MarketError.TradeNotFilled;
        }
    }

    /// <summary>设置 Unit 贸易策略（WaitBuyFood / WaitSellFood / None）。</summary>
    public GameResult<StrategyWorldStateDto> SetUnitTradePolicy(
        int unitId,
        UnitTradePolicy policy,
        int limitPriceMoneyPerGo,
        int quantityGo = 0)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            if (!simulation.World.GameData.Units.TryGetValue(unitId, out var unit))
                return GameError.UnitError.UnitNotFound;

            var ownership = PlayerUnitControlRules.ValidateOwnership(unit, simulation.ScenarioMeta);
            if (!ownership.IsSuccess)
                return ownership.Error!;

            unit.TradePolicy = policy;
            unit.TradeLimitPriceMoneyPerGo = Math.Max(0, limitPriceMoneyPerGo);
            unit.TradeQuantityGo = Math.Max(0, quantityGo);
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

            var control = PlayerUnitControlRules.ValidateDirectUnitCommand(
                unit,
                simulation.ScenarioMeta,
                simulation.World.GameData);
            if (!control.IsSuccess)
                return control.Error!;

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
            var ownership = PlayerUnitControlRules.ValidateOwnership(unit, meta);
            if (!ownership.IsSuccess)
                return ownership.Error!;
            var helper = simulation.Services.GetRequiredService<MessageCarrierDispatchHelper>();
            var issuer = StrategyLordHelper.ResolvePolicyIssuerLocation(unit, gameData, meta);
            var strongholdId = StrategyLordHelper.ResolveSourceStrongholdId(gameData, meta, issuer);

            var outcome = helper.IssuePolicyChange(issuer, strongholdId, unit, directive);

            simulation.MovementTrace.Log(
                "PolicyChange",
                outcome == MessageCarrierDispatchOutcome.AppliedImmediately ? "方针即时生效" : "方针信使派出",
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

            var control = PlayerUnitControlRules.ValidateDirectUnitCommand(
                unit,
                simulation.ScenarioMeta,
                simulation.World.GameData);
            if (!control.IsSuccess)
                return control.Error!;

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

            var control = PlayerUnitControlRules.ValidateDirectUnitCommand(
                unit,
                simulation.ScenarioMeta,
                simulation.World.GameData);
            if (!control.IsSuccess)
                return control.Error!;

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

            var ownership = PlayerUnitControlRules.ValidateOwnership(unit, simulation.ScenarioMeta);
            if (!ownership.IsSuccess)
                return ownership.Error!;

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

    /// <summary>玩家当主自据点出城。</summary>
    public GameResult<StrategyWorldStateDto> OrderCharacterLeaveStronghold(int characterId, bool force = false)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var meta = simulation.ScenarioMeta;
            var gameData = simulation.World.GameData;
            if (!gameData.Characters.TryGetValue(characterId, out var character))
                return GameError.CharacterError.CharacterNotFound;

            var gateAp = simulation.Services.GetRequiredService<GameRuleConfig>().EnterStrongholdAp;
            var result = CharacterPlayerActions.TryLeaveStronghold(
                simulation.GameContext.GameWorldContext,
                character,
                gameData,
                meta,
                gateAp,
                force,
                gameData.SimulationSeed,
                out var riskMessage);
            if (!result.IsSuccess)
                return result.Error!;

            simulation.MovementTrace.Log(
                "CharacterLeave",
                riskMessage ?? "当主出城",
                characterId,
                character.Location,
                character.Location,
                $"stronghold={character.StrongholdId} ap={character.Ap} force={force}");

            return BuildStateResult();
        }
    }

    /// <summary>玩家当主寻路移动（可经中继格）。</summary>
    public GameResult<StrategyWorldStateDto> OrderCharacterMove(
        int characterId,
        Point2 target,
        IReadOnlyList<Point2>? via = null)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var meta = simulation.ScenarioMeta;
            var gameData = simulation.World.GameData;
            if (!gameData.Characters.TryGetValue(characterId, out var character))
                return GameError.CharacterError.CharacterNotFound;

            var pathfinding = simulation.Services.GetRequiredService<IPathfindingService>();
            var result = CharacterPlayerActions.TryOrderMove(
                simulation.GameContext.GameWorldContext,
                character,
                gameData,
                meta,
                pathfinding,
                target,
                via);
            if (!result.IsSuccess)
                return result.Error!;

            var routeText = string.Join(" -> ", character.ActionTarget.RoutePoints.Select(p => p.ToString()));
            simulation.MovementTrace.Log(
                "CharacterMove",
                "当主移动",
                characterId,
                character.Location,
                target,
                $"route=[{routeText}] via={via?.Count ?? 0}");

            return BuildStateResult();
        }
    }

    /// <summary>玩家当主在同格据点入城。</summary>
    public GameResult<StrategyWorldStateDto> OrderCharacterEnterStronghold(
        int characterId,
        int strongholdId,
        bool force = false)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var meta = simulation.ScenarioMeta;
            var gameData = simulation.World.GameData;
            if (!gameData.Characters.TryGetValue(characterId, out var character))
                return GameError.CharacterError.CharacterNotFound;

            if (!gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            var gateAp = simulation.Services.GetRequiredService<GameRuleConfig>().EnterStrongholdAp;
            var result = CharacterPlayerActions.TryEnterStronghold(
                character,
                stronghold,
                gameData,
                meta,
                gateAp,
                force,
                gameData.SimulationSeed,
                out var riskMessage);
            if (!result.IsSuccess)
                return result.Error!;

            simulation.MovementTrace.Log(
                "CharacterEnter",
                riskMessage ?? "当主入城",
                characterId,
                character.Location,
                stronghold.Location,
                $"stronghold={strongholdId} ap={character.Ap} force={force}");

            return BuildStateResult();
        }
    }

    /// <summary>预览玩家当主寻路（不修改状态）。</summary>
    public GameResult<StrategyPathPreviewDto> PreviewCharacterPath(
        int characterId,
        Point2 target,
        Point2? from = null,
        IReadOnlyList<Point2>? via = null)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            if (!simulation.World.GameData.Characters.TryGetValue(characterId, out var character))
                return GameError.CharacterError.CharacterNotFound;

            var pathfinding = simulation.Services.GetRequiredService<IPathfindingService>();
            var gameData = simulation.World.GameData;
            var start = from ?? CharacterPlayerActions.ResolveMoveStartPoint(character, gameData);
            var stops = BuildStopList(start, target, via);
            var path = CharacterPlayerActions.BuildPathThrough(pathfinding, character, stops, start);
            if (!path.IsSuccess)
                return path.Error!;

            var pathPoints = path.Value!
                .Select(node => new StrategyMapPointDto { X = node.Location.X, Y = node.Location.Y })
                .ToList();

            return new StrategyPathPreviewDto { Points = pathPoints };
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

            if (!simulation.World.GameData.Units.TryGetValue(unitId, out var unit))
                return GameError.UnitError.UnitNotFound;

            var ownership = PlayerUnitControlRules.ValidateOwnership(unit, simulation.ScenarioMeta);
            if (!ownership.IsSuccess)
                return ownership.Error!;

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

            if (!simulation.World.GameData.Units.TryGetValue(unitId, out var unit))
                return GameError.UnitError.UnitNotFound;

            var ownership = PlayerUnitControlRules.ValidateOwnership(unit, simulation.ScenarioMeta);
            if (!ownership.IsSuccess)
                return ownership.Error!;

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
    public GameResult<StrategyAdvanceDayResponseDto> AdvanceDay() => AdvanceDays(1);

    /// <summary>在一次锁与一次最终 DTO 映射内推进多日；主要用于观战和长局验证。</summary>
    public GameResult<StrategyAdvanceDayResponseDto> AdvanceDays(int days)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;
            if (days is < 1 or > 31)
                return GameError.InvalidArgument;

            var dayOutcomeBuffer = simulation.Services.GetRequiredService<StrategyDayOutcomeBuffer>();
            var dayDebugLog = simulation.Services.GetRequiredService<IStrategyDayDebugLog>();
            var resolvedBattles = new List<StrategyBattleResultDto>();
            var events = new List<StrategyEventDto>();

            for (var day = 0; day < days; day++)
            {
                dayOutcomeBuffer.Clear();
                var upcoming = simulation.World.GameData.GameDate.AddDays(1);
                dayDebugLog.BeginDay(upcoming.Year, upcoming.Month, upcoming.Day, LoadedScenarioId);

                timeController.AdvanceDay(simulation.World, simulation.Engine);

                dayDebugLog.EndDay(dayOutcomeBuffer.ResolvedBattles.Count, dayOutcomeBuffer.Events.Count);
                resolvedBattles.AddRange(dayOutcomeBuffer.ResolvedBattles);
                events.AddRange(dayOutcomeBuffer.Events);
                simulation.MovementTrace.Log("AdvanceDay", "日推进完成", detail:
                    $"{simulation.World.GameData.GameDate.Year}-{simulation.World.GameData.GameDate.Month}-{simulation.World.GameData.GameDate.Day} battles={dayOutcomeBuffer.ResolvedBattles.Count}");
            }

            var world = BuildStateResult();
            if (!world.IsSuccess)
                return world.Error!;

            return new StrategyAdvanceDayResponseDto
            {
                State = world.Value!,
                ResolvedBattles = resolvedBattles,
                Events = events,
                DaysAdvanced = days,
                DayDebugLogPath = dayDebugLog.LastWrittenFilePath,
                DayDebugEntryCount = dayDebugLog.Snapshot().Count
            };
        }
    }

    /// <summary>
    /// 配置本房间当前由真人占用的势力。未列出的势力继续由 AI 接管。
    /// </summary>
    public GameResult ConfigureHumanControlledForces(IEnumerable<int> forceIds)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var normalized = forceIds.Distinct().OrderBy(id => id).ToHashSet();
            if (normalized.Any(id => !simulation.World.GameData.Forces.ContainsKey(id)))
                return GameError.ForceError.ForceNotFound;

            simulation.ScenarioMeta.HumanControlledForceIds = normalized;
            return GameResult.Ok();
        }
    }

    /// <summary>
    /// 临时把当前请求切换为指定势力的玩家视角。调用方必须保证同一房间请求串行，
    /// 并在命令或 DTO 映射结束后释放返回的作用域。
    /// </summary>
    public GameResult<IDisposable> UsePlayerForce(int forceId)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var gameData = simulation.World.GameData;
            if (!gameData.Forces.TryGetValue(forceId, out var force))
                return GameError.ForceError.ForceNotFound;

            var meta = simulation.ScenarioMeta;
            var snapshot = new PlayerForceContextSnapshot(
                meta.PlayerForceId,
                meta.LordName,
                meta.LordUnitId,
                meta.LordStrongholdId);

            meta.PlayerForceId = forceId;
            var lordId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(forceId, meta, gameData);
            gameData.Characters.TryGetValue(lordId, out var lord);
            meta.LordName = lord?.Name ?? force.Name;
            meta.LordUnitId = lord is null
                ? null
                : gameData.Units.Values
                    .Where(unit => unit.ForceId == forceId && unit.LeaderId == lord.Id)
                    .OrderBy(unit => unit.Id)
                    .Select(unit => (int?)unit.Id)
                    .FirstOrDefault();
            meta.LordStrongholdId = meta.ForceLordResidenceStrongholdIds.TryGetValue(forceId, out var residenceId)
                ? residenceId
                : gameData.Strongholds.Values
                    .Where(stronghold => stronghold.ForceId == forceId)
                    .OrderBy(stronghold => stronghold.Id)
                    .Select(stronghold => (int?)stronghold.Id)
                    .FirstOrDefault();

            return new PlayerForceContextLease(this, snapshot);
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

            var save = StrategyWorldSaveService.Capture(
                simulation.World,
                LoadedScenarioId,
                simulation.ScenarioMeta.PlayerForceId,
                simulation.Services.GetRequiredService<StrategyVisibilityLedger>(),
                simulation.ScenarioMeta);
            save.RuntimeServices = StrategyRuntimeServicesSaveService.Capture(simulation.Services);
            return save;
        }
    }

    /// <summary>从存档恢复：先加载剧本再覆盖可变状态。</summary>
    public GameResult<StrategyWorldStateDto> RestoreSave(StrategySaveDocument save)
    {
        lock (sync)
        {
            if (string.IsNullOrWhiteSpace(save.ScenarioId))
                return GameError.DataNotFound;

            var difficulty = Enum.TryParse<StrategyDifficulty>(
                save.Difficulty,
                ignoreCase: true,
                out var savedDifficulty)
                ? savedDifficulty
                : (StrategyDifficulty?)null;
            var loadOptions = difficulty is null && save.AllForcesAiControlled is null
                ? null
                : new StrategyLoadOptions
                {
                    Difficulty = difficulty,
                    CustomStartOptions = difficulty == StrategyDifficulty.Custom && save.StartOptions is not null
                        ? GameStartOptionsMapper.FromDto(save.StartOptions)
                        : null,
                    AllForcesAiControlled = save.AllForcesAiControlled ?? false,
                };

            var loadResult = LoadScenario(save.ScenarioId, loadOptions);
            if (!loadResult.IsSuccess)
                return loadResult.Error!;

            if (simulation is null)
                return GameError.DataNotFound;

            StrategyWorldSaveService.Apply(save, simulation.World);
            StrategyRuntimeServicesSaveService.TryRestore(save.RuntimeServices, simulation.Services);
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

    private void RestorePlayerForceContext(PlayerForceContextSnapshot snapshot)
    {
        lock (sync)
        {
            if (simulation is null)
                return;

            var meta = simulation.ScenarioMeta;
            meta.PlayerForceId = snapshot.PlayerForceId;
            meta.LordName = snapshot.LordName;
            meta.LordUnitId = snapshot.LordUnitId;
            meta.LordStrongholdId = snapshot.LordStrongholdId;
        }
    }

    private sealed record PlayerForceContextSnapshot(
        int PlayerForceId,
        string LordName,
        int? LordUnitId,
        int? LordStrongholdId);

    private sealed class PlayerForceContextLease(
        StrategySimulationHost owner,
        PlayerForceContextSnapshot snapshot) : IDisposable
    {
        private StrategySimulationHost? currentOwner = owner;

        public void Dispose()
        {
            var toRestore = Interlocked.Exchange(ref currentOwner, null);
            toRestore?.RestorePlayerForceContext(snapshot);
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

    /// <summary>调整据点税率；当主须在居城，仅直辖城可调整，税令经信使传达后生效。</summary>
    public GameResult<StrategyPolicyChangeResponseDto> SetStrongholdTaxRates(
        int strongholdId,
        byte? pollTaxRate,
        byte? agricultureTaxRate,
        byte? commerceTaxRate,
        byte? tariffTaxRate)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var meta = simulation.ScenarioMeta;
            var gameData = simulation.World.GameData;
            if (!gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            if (!StrongholdDomesticRules.IsPlayerRealmStronghold(stronghold, meta, gameData))
                return GameError.DiplomacyError.NotSelfForce;

            if (!StrongholdDomesticRules.CanPlayerAdjustTaxRates(stronghold, meta, gameData))
                return GameError.DomesticError.AppointedLordTerritory;

            if (!StrongholdDomesticRules.CanLordCommandAtStronghold(meta, gameData, stronghold))
                return GameError.DomesticError.LordNotAtResidence;

            var taxChange = new PendingStrongholdTaxChange
            {
                PollTaxRate = pollTaxRate,
                AgricultureTaxRate = agricultureTaxRate,
                CommerceTaxRate = commerceTaxRate,
                TariffTaxRate = tariffTaxRate
            };

            if (!StrongholdDomesticActions.TryValidateTaxRates(taxChange, out var validationError))
                return validationError ?? GameError.DataNotFound;

            var residenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
                meta.PlayerForceId,
                gameData,
                meta);
            if (residenceId <= 0
                || !gameData.Strongholds.TryGetValue(residenceId, out var residence))
            {
                return GameError.StrongholdError.StrongholdNotFound;
            }

            var helper = simulation.Services.GetRequiredService<MessageCarrierDispatchHelper>();
            var outcome = helper.IssueTaxRateChange(
                residence.Location,
                residenceId,
                stronghold,
                taxChange);

            simulation.MovementTrace.Log(
                "TaxRateChange",
                outcome == MessageCarrierDispatchOutcome.AppliedImmediately ? "税率即时生效" : "税令信使派出",
                strongholdId,
                residence.Location,
                stronghold.Location,
                $"poll={pollTaxRate} agri={agricultureTaxRate} commerce={commerceTaxRate} tariff={tariffTaxRate} outcome={outcome}");

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

    /// <summary>据点征兵；当主须在居城。改为向将领派发征兵任务。</summary>
    public GameResult<StrategyWorldStateDto> RecruitAtStronghold(int strongholdId, int characterId)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var meta = simulation.ScenarioMeta;
            var gameData = simulation.World.GameData;
            if (!gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            if (!StrongholdRecruitTaskRules.CanAssignRecruitTaskAt(stronghold, meta, gameData))
                return GameError.DiplomacyError.NotSelfForce;

            if (!StrongholdDomesticRules.CanLordCommandAtStronghold(meta, gameData, stronghold))
                return GameError.DomesticError.LordNotAtResidence;

            var error = StrongholdRecruitTaskActions.TryAssignConscriptRecruitTask(
                stronghold,
                characterId,
                gameData,
                meta);
            if (error is not null)
                return error;

            if (gameData.Forces.TryGetValue(stronghold.ForceId, out var force))
                ForceEconomyActions.SyncForceTreasuryFromStrongholds(force, gameData);

            return BuildStateResult();
        }
    }

    /// <summary>据点募兵；当主须在居城，将领携预算执行任务。</summary>
    public GameResult<StrategyWorldStateDto> MercenaryRecruitAtStronghold(
        int strongholdId,
        int characterId,
        int budgetMoney)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var meta = simulation.ScenarioMeta;
            var gameData = simulation.World.GameData;
            if (!gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            if (!StrongholdRecruitTaskRules.CanAssignRecruitTaskAt(stronghold, meta, gameData))
                return GameError.DiplomacyError.NotSelfForce;

            if (!StrongholdDomesticRules.CanLordCommandAtStronghold(meta, gameData, stronghold))
                return GameError.DomesticError.LordNotAtResidence;

            var error = StrongholdRecruitTaskActions.TryAssignMercenaryRecruitTask(
                stronghold,
                characterId,
                budgetMoney,
                gameData,
                meta);
            if (error is not null)
                return error;

            if (gameData.Forces.TryGetValue(stronghold.ForceId, out var force))
                ForceEconomyActions.SyncForceTreasuryFromStrongholds(force, gameData);

            return BuildStateResult();
        }
    }

    /// <summary>设置据点政务方针；当主须能指挥该本家据点，异格据点经信使传达后生效。</summary>
    public GameResult<StrategyPolicyChangeResponseDto> SetStrongholdGovernancePriority(
        int strongholdId,
        StrongholdGovernancePriority priority)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var meta = simulation.ScenarioMeta;
            var gameData = simulation.World.GameData;
            if (!gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            if (!StrongholdGovernanceRules.CanPlayerConfigureGovernancePolicy(stronghold, meta, gameData))
                return GameError.DiplomacyError.NotSelfForce;

            if (!StrongholdDomesticRules.CanLordCommandAtStronghold(meta, gameData, stronghold))
                return GameError.DomesticError.LordNotAtResidence;

            if (!Enum.IsDefined(priority))
                return GameError.DataNotFound;

            var residenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
                meta.PlayerForceId,
                gameData,
                meta);
            if (residenceId <= 0
                || !gameData.Strongholds.TryGetValue(residenceId, out var residence))
            {
                return GameError.StrongholdError.StrongholdNotFound;
            }

            var governanceChange = new PendingStrongholdGovernanceChange { Priority = priority };
            var helper = simulation.Services.GetRequiredService<MessageCarrierDispatchHelper>();
            var outcome = helper.IssueGovernancePriorityChange(
                residence.Location,
                residenceId,
                stronghold,
                governanceChange,
                meta);

            simulation.MovementTrace.Log(
                "GovernancePriorityChange",
                outcome == MessageCarrierDispatchOutcome.AppliedImmediately ? "方针即时生效" : "方针信使派出",
                strongholdId,
                residence.Location,
                stronghold.Location,
                $"priority={priority} outcome={outcome}");

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

    /// <summary>角色个人征兵：领主/代官/当主在城内亲自执行。</summary>
    public GameResult<StrategyWorldStateDto> PersonalRecruit(int characterId)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var meta = simulation.ScenarioMeta;
            var gameData = simulation.World.GameData;
            if (!TryResolvePersonalCommandStronghold(characterId, gameData, out var character, out var stronghold))
                return GameError.DomesticError.CharacterNotAtStronghold;

            if (!StrongholdRecruitTaskRules.CanAssignRecruitTaskAt(stronghold, meta, gameData))
                return GameError.DiplomacyError.NotSelfForce;

            var error = StrongholdRecruitTaskActions.TryAssignPersonalConscriptRecruitTask(
                stronghold,
                character,
                gameData,
                meta);
            if (error is not null)
                return error;

            return BuildStateResult();
        }
    }

    /// <summary>角色个人募兵：预算从执行者个人金库扣除。</summary>
    public GameResult<StrategyWorldStateDto> PersonalMercenaryRecruit(int characterId, int budgetMoney)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var meta = simulation.ScenarioMeta;
            var gameData = simulation.World.GameData;
            if (!TryResolvePersonalCommandStronghold(characterId, gameData, out var character, out var stronghold))
                return GameError.DomesticError.CharacterNotAtStronghold;

            if (!StrongholdRecruitTaskRules.CanAssignRecruitTaskAt(stronghold, meta, gameData))
                return GameError.DiplomacyError.NotSelfForce;

            var error = StrongholdRecruitTaskActions.TryAssignPersonalMercenaryRecruitTask(
                stronghold,
                character,
                budgetMoney,
                gameData,
                meta);
            if (error is not null)
                return error;

            return BuildStateResult();
        }
    }

    private static bool TryResolvePersonalCommandStronghold(
        int characterId,
        GameData gameData,
        out Character character,
        out Stronghold stronghold)
    {
        character = null!;
        stronghold = null!;
        if (characterId <= 0
            || !gameData.Characters.TryGetValue(characterId, out var resolved)
            || resolved.IsDead
            || resolved.LocationType != Character.CharacterLocationType.Stronghold
            || resolved.LocationStrongholdId <= 0
            || !gameData.Strongholds.TryGetValue(resolved.LocationStrongholdId, out var locatedStronghold))
        {
            return false;
        }

        character = resolved;
        stronghold = locatedStronghold;
        return true;
    }

    /// <summary>任命据点领主/代官；领主任命中当主 Id 表示设为直辖。</summary>
    public GameResult<StrategyWorldStateDto> AppointStrongholdLord(
        int strongholdId,
        int characterId,
        string appointType = "Lord")
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var meta = simulation.ScenarioMeta;
            var gameData = simulation.World.GameData;
            if (!gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            var pathfinding = simulation.Services.GetRequiredService<IPathfindingService>();
            var lordRegistry = simulation.Services.GetRequiredService<StrategyForceLordRegistry>();
            var isMayor = string.Equals(appointType, "Mayor", StringComparison.OrdinalIgnoreCase);
            var error = isMayor
                ? StrongholdLordActions.TryAppointMayor(
                    stronghold,
                    characterId,
                    gameData,
                    meta,
                    simulation.GameContext.GameWorldContext,
                    pathfinding,
                    lordRegistry)
                : StrongholdLordActions.TryAppointLord(
                    stronghold,
                    characterId,
                    gameData,
                    meta,
                    simulation.GameContext.GameWorldContext,
                    pathfinding,
                    lordRegistry);

            if (error is not null)
                return error;

            return BuildStateResult();
        }
    }

    /// <summary>调动将领：派遣（自本据点）或召集（至本据点）。</summary>
    public GameResult<StrategyWorldStateDto> TransferCharacterToStronghold(
        int strongholdId,
        int characterId,
        string mode,
        int destinationStrongholdId = 0)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var meta = simulation.ScenarioMeta;
            var gameData = simulation.World.GameData;
            if (!gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            GameError? error;
            if (string.Equals(mode, "Dispatch", StringComparison.OrdinalIgnoreCase))
            {
                if (destinationStrongholdId <= 0
                    || !gameData.Strongholds.TryGetValue(destinationStrongholdId, out var destination))
                {
                    return GameError.StrongholdError.StrongholdNotFound;
                }

                error = StrongholdPersonnelActions.TryDispatchCharacter(
                    stronghold,
                    destination,
                    characterId,
                    gameData,
                    meta);
            }
            else
            {
                error = StrongholdPersonnelActions.TryTransferCharacter(
                    stronghold,
                    characterId,
                    gameData,
                    meta);
            }

            if (error is not null)
                return error;

            return BuildStateResult();
        }
    }

    /// <summary>召回外派任务的将领；自居城派出信使，同格即时生效。</summary>
    public GameResult<StrategyPolicyChangeResponseDto> RecallCharacter(int strongholdId, int characterId)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var meta = simulation.ScenarioMeta;
            var gameData = simulation.World.GameData;
            if (!gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
                return GameError.StrongholdError.StrongholdNotFound;

            if (!StrongholdDomesticRules.IsPlayerRealmStronghold(stronghold, meta, gameData))
                return GameError.DiplomacyError.NotSelfForce;

            if (!StrongholdDomesticRules.CanLordCommandAtStronghold(meta, gameData, stronghold))
                return GameError.DomesticError.LordNotAtResidence;

            if (characterId <= 0 || !gameData.Characters.TryGetValue(characterId, out var character) || character.IsDead)
                return GameError.DataNotFound;

            if (character.ForceId != stronghold.ForceId)
                return GameError.DiplomacyError.NotSelfForce;

            if (!StrongholdPersonnelActions.HasRecallableTask(character))
                return GameError.DomesticError.CharacterNotOnRecallableTask;

            var residenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
                meta.PlayerForceId,
                gameData,
                meta);
            if (residenceId <= 0
                || !gameData.Strongholds.TryGetValue(residenceId, out var residence))
            {
                return GameError.StrongholdError.StrongholdNotFound;
            }

            var helper = simulation.Services.GetRequiredService<MessageCarrierDispatchHelper>();
            var dayOutcomeBuffer = simulation.Services.GetRequiredService<StrategyDayOutcomeBuffer>();
            var outcome = helper.IssueCharacterRecall(
                residence.Location,
                residenceId,
                character,
                gameData,
                meta);

            if (outcome == MessageCarrierDispatchOutcome.AppliedImmediately
                && character.ForceId == meta.PlayerForceId)
            {
                dayOutcomeBuffer.AddEvent(new StrategyEventDto
                {
                    Category = "CharacterRecallDelivered",
                    Message = $"📨 召回令已传达，{character.Name} 正尽快回城"
                });
            }

            simulation.MovementTrace.Log(
                "CharacterRecall",
                outcome == MessageCarrierDispatchOutcome.AppliedImmediately ? "召回令即时传达" : "召回令信使派出",
                characterId,
                residence.Location,
                StrongholdPersonnelActions.ResolveCharacterDeliveryLocation(character, gameData),
                $"character={character.Name} outcome={outcome}");

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

    /// <summary>设置两势力外交关系（宣战/议和/同盟）。</summary>
    public GameResult<StrategyWorldStateDto> SetDiplomacyRelation(
        int targetForceId,
        string relation)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            if (!Enum.TryParse<Diplomacy.DiplomacyRelation>(relation, ignoreCase: true, out var rel))
                return GameError.DataNotFound;

            var meta = simulation.ScenarioMeta;
            var gameData = simulation.World.GameData;
            if (!ForceDiplomacyActions.TrySetRelation(
                    gameData, meta.PlayerForceId, targetForceId, rel, out var error))
            {
                return error ?? GameError.DataNotFound;
            }

            return BuildStateResult();
        }
    }

    /// <summary>外交：支配/从属外藩。</summary>
    public GameResult<StrategyWorldStateDto> OrderDiplomacyVassalage(
        int targetForceId,
        string action)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var meta = simulation.ScenarioMeta;
            var gameData = simulation.World.GameData;
            var playerForceId = meta.PlayerForceId;
            string? error = null;
            var ok = action.ToLowerInvariant() switch
            {
                "impose" => ForceDiplomacyActions.TryImposeOuterVassalage(
                    gameData, playerForceId, targetForceId, out error),
                "submit" => ForceDiplomacyActions.TrySubmitOuterVassalage(
                    gameData, playerForceId, targetForceId, out error),
                "release" => ForceDiplomacyActions.TryReleaseVassal(
                    gameData, playerForceId, targetForceId, out error),
                "independence" => ForceDiplomacyActions.TryDeclareIndependence(
                    gameData, playerForceId, out error),
                _ => false
            };

            if (!ok)
                return error ?? GameError.DataNotFound;

            return BuildStateResult();
        }
    }

    /// <summary>外政：任命/撤销内藩。</summary>
    public GameResult<StrategyWorldStateDto> OrderRealmInnerVassal(
        int targetForceId,
        string action)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var meta = simulation.ScenarioMeta;
            var gameData = simulation.World.GameData;
            var playerForceId = meta.PlayerForceId;
            string? error = null;
            var ok = action.ToLowerInvariant() switch
            {
                "appoint" => ForceDiplomacyActions.TryAppointInnerVassal(
                    gameData, playerForceId, targetForceId, out error),
                "revoke" => ForceDiplomacyActions.TryRevokeInnerVassal(
                    gameData, playerForceId, targetForceId, out error),
                _ => false
            };

            if (!ok)
                return error ?? GameError.DataNotFound;

            return BuildStateResult();
        }
    }

    /// <summary>预览外交使节任务成功率与行程。</summary>
    public GameResult<StrategyDiplomacyMissionPreviewDto> PreviewDiplomacyMission(
        int characterId,
        int targetForceId,
        string action)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var meta = simulation.ScenarioMeta;
            var gameData = simulation.World.GameData;
            var playerForceId = meta.PlayerForceId;

            if (!DiplomacyMissionRules.TryParseAction(action, out var normalizedAction))
                return GameError.DiplomacyError.InvalidForce;

            if (!DiplomacyMissionRules.CanAssignMissionTarget(
                    gameData, meta, playerForceId, targetForceId, normalizedAction, out var assignError))
            {
                return assignError ?? GameError.DiplomacyError.InvalidForce;
            }

            var travelDays = DiplomacyMissionRules.EstimateTravelDays(
                gameData,
                meta,
                playerForceId,
                targetForceId);
            var idleOfficers = DiplomacyMissionRules.ListAssignableEnvoys(gameData, meta, playerForceId)
                .Select(c => new StrategyDiplomacyMissionOfficerDto
                {
                    CharacterId = c.Id,
                    Name = c.Name,
                })
                .ToList();

            var successChance = 0;
            if (characterId > 0
                && gameData.Characters.TryGetValue(characterId, out var envoy)
                && envoy.ForceId == playerForceId)
            {
                successChance = DiplomacyMissionRules.EstimateSuccessChancePercent(
                    envoy,
                    gameData,
                    playerForceId,
                    targetForceId,
                    action);
            }

            return new StrategyDiplomacyMissionPreviewDto
            {
                SuccessChancePercent = successChance,
                TravelDays = travelDays,
                IdleOfficers = idleOfficers,
            };
        }
    }

    /// <summary>派遣外交使节任务（同盟/宣战/议和）。</summary>
    public GameResult<StrategyWorldStateDto> OrderDiplomacyMission(
        int characterId,
        int targetForceId,
        string action)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var meta = simulation.ScenarioMeta;
            var gameData = simulation.World.GameData;

            if (!DiplomacyMissionActions.TryAssignMission(
                    gameData,
                    meta,
                    characterId,
                    targetForceId,
                    action,
                    out var error))
            {
                return error ?? GameError.DataNotFound;
            }

            return BuildStateResult();
        }
    }

    /// <summary>玩家当主与同地人物交谈或赠礼，立即更新双向关系。</summary>
    public GameResult<StrategyWorldStateDto> OrderCharacterInteraction(
        int characterId,
        int targetCharacterId,
        string interaction)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var result = CharacterSocialActions.TryInteract(
                simulation.World.GameData,
                simulation.ScenarioMeta,
                characterId,
                targetCharacterId,
                interaction,
                out var message);
            if (!result.IsSuccess)
                return result.Error!;

            simulation.MovementTrace.Log(
                "CharacterInteraction",
                message,
                characterId,
                detail: $"target={targetCharacterId} interaction={interaction}");
            return BuildStateResult();
        }
    }

    /// <summary>预览多条款和谈的战争分数成本与接受率。</summary>
    public GameResult<StrategyPeaceSettlementPreviewDto> PreviewPeaceSettlement(
        int characterId,
        int targetForceId,
        StrategyPeaceTermsDto termsDto)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            var meta = simulation.ScenarioMeta;
            var gameData = simulation.World.GameData;
            if (!gameData.Characters.TryGetValue(characterId, out var envoy)
                || envoy.ForceId != meta.PlayerForceId)
            {
                return GameError.DiplomacyError.NotSelfForce;
            }

            if (!DiplomacyMissionRules.CanAssignMissionTarget(
                    gameData,
                    meta,
                    meta.PlayerForceId,
                    targetForceId,
                    "Peace",
                    out var assignError))
            {
                return assignError ?? GameError.DiplomacyError.InvalidForce;
            }

            var baseChance = DiplomacyMissionRules.EstimateSuccessChancePercent(
                envoy,
                gameData,
                meta.PlayerForceId,
                targetForceId,
                "Peace");
            if (!PeaceSettlementRules.TryBuildPreview(
                    gameData,
                    meta.PlayerForceId,
                    targetForceId,
                    PeaceSettlementRules.ToDomainTerms(termsDto),
                    baseChance,
                    out var preview,
                    out var error))
            {
                return error ?? GameError.DiplomacyError.InvalidForce;
            }

            return preview;
        }
    }

    /// <summary>派遣携带多条款和谈书的使节。</summary>
    public GameResult<StrategyWorldStateDto> OrderPeaceSettlement(
        int characterId,
        int targetForceId,
        StrategyPeaceTermsDto termsDto)
    {
        lock (sync)
        {
            if (simulation is null)
                return GameError.DataNotFound;

            if (!DiplomacyMissionActions.TryAssignMission(
                    simulation.World.GameData,
                    simulation.ScenarioMeta,
                    characterId,
                    targetForceId,
                    "Peace",
                    PeaceSettlementRules.ToDomainTerms(termsDto),
                    out var error))
            {
                return error ?? GameError.DataNotFound;
            }

            return BuildStateResult();
        }
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
