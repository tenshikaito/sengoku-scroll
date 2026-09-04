using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Persistence;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.WebApi.Models;
using SengokuScroll.WebApi.Multiplayer;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.WebApi.Controllers;

/// <summary>策略模式 API（M2-a）：命令成功后统一返回 <see cref="StrategyWorldStateDto"/>。</summary>
[ApiController]
[Route("strategy")]
[Route("api/strategy")]
public class StrategyController : ControllerBase, IAsyncActionFilter
{
    private readonly StrategySimulationHost defaultSimulationHost;
    private readonly StrategySaveSlotRepository saveSlotRepository;
    private readonly ILogger<StrategyController> logger;
    private readonly StrategyMultiplayerRoomManager multiplayerRooms;
    private readonly IHubContext<StrategyRoomHub> roomHub;
    private StrategySimulationHost? multiplayerSimulationHost;

    private StrategySimulationHost simulationHost
        => multiplayerSimulationHost ?? defaultSimulationHost;

    public StrategyController(
        StrategySimulationHost simulationHost,
        StrategySaveSlotRepository saveSlotRepository,
        ILogger<StrategyController> logger,
        StrategyMultiplayerRoomManager multiplayerRooms,
        IHubContext<StrategyRoomHub> roomHub)
    {
        defaultSimulationHost = simulationHost;
        this.saveSlotRepository = saveSlotRepository;
        this.logger = logger;
        this.multiplayerRooms = multiplayerRooms;
        this.roomHub = roomHub;
    }

    [NonAction]
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var roomId = Request.Headers[StrategyMultiplayerHeaders.RoomId].FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(roomId))
        {
            await next();
            return;
        }

        if (!multiplayerRooms.TryGetRoom(roomId, out var room))
        {
            context.Result = NotFound(new ApiErrorResponse("RoomNotFound"));
            return;
        }

        var playerToken = Request.Headers[StrategyMultiplayerHeaders.PlayerToken].FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(playerToken))
        {
            context.Result = Unauthorized(new ApiErrorResponse("MissingPlayerToken"));
            return;
        }

        var path = Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (IsBlockedMultiplayerPath(path))
        {
            context.Result = StatusCode(
                StatusCodes.Status403Forbidden,
                new ApiErrorResponse("MultiplayerOperationNotAllowed"));
            return;
        }

        await room.Gate.WaitAsync(HttpContext.RequestAborted);
        string? reservedCommandId = null;
        var commandReserved = false;
        var commandSucceeded = false;
        try
        {
            if (!room.TryAuthenticate(playerToken, out var player))
            {
                context.Result = Unauthorized(new ApiErrorResponse("InvalidRoomCredentials"));
                return;
            }

            room.MarkConnected(player);
            room.RefreshHumanControlledForces();
            var isMutation = IsMutatingRequest(Request.Method, path);
            if (isMutation)
            {
                reservedCommandId = Request.Headers[StrategyMultiplayerHeaders.CommandId]
                    .FirstOrDefault()?.Trim();
                if (string.IsNullOrWhiteSpace(reservedCommandId) || reservedCommandId.Length > 100)
                {
                    context.Result = BadRequest(new ApiErrorResponse("MissingOrInvalidCommandId"));
                    return;
                }

                if (!room.TryReserveCommandId(reservedCommandId))
                {
                    context.Result = Conflict(new ApiErrorResponse("DuplicateCommand"));
                    return;
                }
                commandReserved = true;
            }

            var playerContext = room.Host.UsePlayerForce(player.ForceId);
            if (!playerContext.IsSuccess)
            {
                context.Result = BadRequest(new ApiErrorResponse(playerContext.Error?.Code ?? "ForceContextFailed"));
                return;
            }

            using (playerContext.Value)
            {
                multiplayerSimulationHost = room.Host;
                var executed = await next();
                commandSucceeded = executed.Exception is null && IsSuccessfulResult(executed.Result);
                if (isMutation && commandSucceeded)
                {
                    room.MarkWorldChanged();
                    await roomHub.Clients.Group(StrategyRoomHub.GroupName(room.RoomId)).SendAsync(
                        "WorldChanged",
                        new
                        {
                            roomId = room.RoomId,
                            worldVersion = room.WorldVersion,
                            reason = "CommandCommitted"
                        },
                        CancellationToken.None);
                }
            }

            Response.Headers[StrategyMultiplayerHeaders.WorldVersion] = room.WorldVersion.ToString();
        }
        finally
        {
            multiplayerSimulationHost = null;
            if (reservedCommandId is not null && commandReserved && !commandSucceeded)
                room.ReleaseCommandId(reservedCommandId);
            room.Gate.Release();
        }
    }

    private static bool IsBlockedMultiplayerPath(string path)
        => path.EndsWith("/load", StringComparison.Ordinal)
           || path.Contains("/advance-day", StringComparison.Ordinal)
           || path.Contains("/advance-days", StringComparison.Ordinal)
           || path.Contains("/restore-save", StringComparison.Ordinal)
           || path.Contains("/save-slots", StringComparison.Ordinal)
           || path.EndsWith("/save", StringComparison.Ordinal)
           || path.Contains("/instant-battle", StringComparison.Ordinal);

    private static bool IsMutatingRequest(string method, string path)
        => !HttpMethods.IsGet(method)
           && !HttpMethods.IsHead(method)
           && !path.Contains("/preview", StringComparison.Ordinal);

    private static bool IsSuccessfulResult(IActionResult? result)
    {
        var statusCode = result switch
        {
            ObjectResult objectResult => objectResult.StatusCode,
            StatusCodeResult statusResult => statusResult.StatusCode,
            _ => null
        };
        return statusCode is null or < StatusCodes.Status400BadRequest;
    }

    /// <summary>加载 JSON 剧本（如 mini_kanto）。</summary>
    [HttpPost("load")]
    public IActionResult Load([FromBody] LoadScenarioRequest request)
    {
        StrategyLoadOptions? loadOptions = null;
        if (!string.IsNullOrWhiteSpace(request.Difficulty)
            || request.CustomStartOptions is not null
            || request.AllForcesAiControlled)
        {
            loadOptions = new StrategyLoadOptions
            {
                Difficulty = string.IsNullOrWhiteSpace(request.Difficulty)
                    ? null
                    : StrategyDifficultyRules.Parse(request.Difficulty),
                CustomStartOptions = request.CustomStartOptions is null
                    ? null
                    : GameStartOptionsMapper.FromDto(request.CustomStartOptions),
                AllForcesAiControlled = request.AllForcesAiControlled
            };
        }

        return ToActionResult(simulationHost.LoadScenario(request.ScenarioId, loadOptions));
    }

    /// <summary>令指定军事单位寻路并向目标格移动（逐日执行）。</summary>
    [HttpPost("units/{unitId:int}/move")]
    public IActionResult MoveUnit(int unitId, [FromBody] MoveUnitRequest request)
        => ToActionResult(simulationHost.OrderUnitMove(
            unitId,
            new Point2(request.X, request.Y),
            ToViaPoints(request.Via)));

    [HttpPost("units/{unitId:int}/preview-path")]
    public IActionResult PreviewPath(int unitId, [FromBody] MoveUnitRequest request)
        => ToPreviewResult(simulationHost.PreviewUnitPath(
            unitId,
            new Point2(request.X, request.Y),
            request.FromX is int fromX && request.FromY is int fromY ? new Point2(fromX, fromY) : null,
            ToViaPoints(request.Via)));

    /// <summary>玩家当主自据点出城。</summary>
    [HttpPost("characters/{characterId:int}/leave-stronghold")]
    public IActionResult LeaveStronghold(int characterId, [FromBody] CharacterGateRequest? request)
        => ToActionResult(simulationHost.OrderCharacterLeaveStronghold(characterId, request?.Force ?? false));

    /// <summary>玩家当主寻路移动。</summary>
    [HttpPost("characters/{characterId:int}/move")]
    public IActionResult MoveCharacter(int characterId, [FromBody] MoveUnitRequest request)
        => ToActionResult(simulationHost.OrderCharacterMove(
            characterId,
            new Point2(request.X, request.Y),
            ToViaPoints(request.Via)));

    /// <summary>玩家当主在同格据点入城。</summary>
    [HttpPost("characters/{characterId:int}/enter-stronghold")]
    public IActionResult EnterStronghold(int characterId, [FromBody] EnterStrongholdRequest request)
        => ToActionResult(simulationHost.OrderCharacterEnterStronghold(
            characterId,
            request.StrongholdId,
            request.Force));

    /// <summary>玩家当主与同地人物交谈或赠礼。</summary>
    [HttpPost("characters/{characterId:int}/interact")]
    public IActionResult InteractWithCharacter(int characterId, [FromBody] CharacterInteractionRequest request)
        => ToActionResult(simulationHost.OrderCharacterInteraction(
            characterId,
            request.TargetCharacterId,
            request.Interaction));

    [HttpPost("characters/{characterId:int}/preview-path")]
    public IActionResult PreviewCharacterPath(int characterId, [FromBody] MoveUnitRequest request)
        => ToPreviewResult(simulationHost.PreviewCharacterPath(
            characterId,
            new Point2(request.X, request.Y),
            request.FromX is int fromX && request.FromY is int fromY ? new Point2(fromX, fromY) : null,
            ToViaPoints(request.Via)));

    /// <summary>预览对目标格敌军的瞬间战（不修改状态）。</summary>
    [HttpPost("units/{unitId:int}/preview-battle")]
    public IActionResult PreviewBattle(int unitId, [FromBody] MoveUnitRequest request)
        => ToBattlePreviewResult(simulationHost.PreviewUnitAttack(unitId, new Point2(request.X, request.Y)));

    /// <summary>对目标格敌军执行瞬间战。</summary>
    [HttpPost("units/{unitId:int}/instant-battle")]
    public IActionResult InstantBattle(int unitId, [FromBody] MoveUnitRequest request)
        => ToBattleResult(simulationHost.ExecuteInstantBattle(unitId, new Point2(request.X, request.Y)));

    /// <summary>变更单位方针（同格即时，异格从当主所在格经信使）。</summary>
    [HttpPost("units/{unitId:int}/directive")]
    public IActionResult SetUnitDirective(int unitId, [FromBody] SetUnitDirectiveRequest request)
    {
        if (!Enum.TryParse<UnitDirective>(request.Directive, ignoreCase: true, out var directive))
            return BadRequest(new ApiErrorResponse("InvalidDirective"));

        return ToPolicyResult(simulationHost.OrderUnitDirective(unitId, directive));
    }

    /// <summary>下达攻击命令（日推进后结算）。</summary>
    [HttpPost("units/{unitId:int}/attack-order")]
    public IActionResult OrderAttack(int unitId, [FromBody] MoveUnitRequest request)
        => ToActionResult(simulationHost.OrderUnitAttack(unitId, new Point2(request.X, request.Y)));

    /// <summary>对敌方据点下达攻城指令（强攻 / 包围，消耗 AP）。</summary>
    [HttpPost("units/{unitId:int}/siege-order")]
    public IActionResult OrderSiege(int unitId, [FromBody] SiegeOrderRequest request)
    {
        if (!Enum.TryParse<UnitSiegeMode>(request.Mode, ignoreCase: true, out var mode)
            || mode == UnitSiegeMode.None)
            return BadRequest(new ApiErrorResponse("InvalidSiegeMode"));

        return ToActionResult(simulationHost.OrderUnitSiege(unitId, request.StrongholdId, mode));
    }

    /// <summary>合并两支友军（同格或邻格）。</summary>
    [HttpPost("units/{sourceUnitId:int}/merge")]
    public IActionResult MergeUnit(int sourceUnitId, [FromBody] MergeUnitRequest request)
        => ToActionResult(simulationHost.OrderUnitMerge(sourceUnitId, request.TargetUnitId));

    /// <summary>拆出子编制并在邻格生成新部队。</summary>
    [HttpPost("units/{unitId:int}/split")]
    public IActionResult SplitUnit(int unitId, [FromBody] SplitUnitRequest request)
        => ToActionResult(simulationHost.OrderUnitSplit(
            unitId,
            request.SubUnitIds,
            new Point2(request.SpawnX, request.SpawnY),
            request.Name));

    /// <summary>从当主居城出征。</summary>
    [HttpPost("strongholds/{strongholdId:int}/deploy")]
    public IActionResult DeployFromStronghold(int strongholdId, [FromBody] DeployFromStrongholdRequest request)
        => ToActionResult(simulationHost.DeployFromStronghold(
            strongholdId,
            request.UnitName ?? string.Empty,
            request.CommanderId,
            request.Composition
                .Select(c => new StrategyDeployCompositionEntry
                {
                    TypeId = c.TypeId,
                    TypeName = c.TypeName,
                    Soldiers = c.Soldiers,
                    CommanderId = c.CommanderId
                })
                .ToList(),
            request.Food,
            request.Money,
            request.DeployToMap));

    /// <summary>单位入城。</summary>
    [HttpPost("units/{unitId:int}/enter-stronghold/{strongholdId:int}")]
    public IActionResult EnterUnitStronghold(int unitId, int strongholdId)
        => ToActionResult(simulationHost.EnterUnitStronghold(unitId, strongholdId));

    /// <summary>单位出城。</summary>
    [HttpPost("units/{unitId:int}/exit-stronghold/{strongholdId:int}")]
    public IActionResult ExitUnitStronghold(int unitId, int strongholdId)
        => ToActionResult(simulationHost.ExitUnitStronghold(unitId, strongholdId));

    /// <summary>建制解散（仅 Home 据点）。</summary>
    [HttpPost("units/{unitId:int}/disband")]
    public IActionResult DisbandUnitOrganizationally(int unitId)
        => ToActionResult(simulationHost.DisbandUnitOrganizationally(unitId));

    /// <summary>创立商店。</summary>
    [HttpPost("strongholds/{strongholdId:int}/shops")]
    public IActionResult CreateMerchantShop(int strongholdId, [FromBody] CreateMerchantShopRequest? request)
        => ToActionResult(simulationHost.CreateMerchantShop(strongholdId, request?.HouseName));

    /// <summary>Unit 市价购粮。</summary>
    [HttpPost("units/{unitId:int}/trade/smash-buy-food")]
    public IActionResult UnitSmashBuyFood(int unitId, [FromBody] UnitSmashBuyFoodRequest request)
        => ToActionResult(simulationHost.UnitSmashBuyFood(
            unitId, request.MaxPriceMoneyPerGo, request.QuantityGo));

    /// <summary>Unit 市价卖出粮食（砸单）。</summary>
    [HttpPost("units/{unitId:int}/trade/smash-sell-food")]
    public IActionResult UnitSmashSellFood(int unitId, [FromBody] UnitSmashSellFoodRequest request)
        => ToActionResult(simulationHost.UnitSmashSellFood(
            unitId, request.MinPriceMoneyPerGo, request.QuantityGo));

    /// <summary>Unit 市价买入马匹。</summary>
    [HttpPost("units/{unitId:int}/trade/smash-buy-horse")]
    public IActionResult UnitSmashBuyHorse(int unitId, [FromBody] UnitSmashBuyFoodRequest request)
        => ToActionResult(simulationHost.UnitSmashBuyHorse(
            unitId, request.MaxPriceMoneyPerGo, request.QuantityGo));

    /// <summary>Unit 市价卖出马匹。</summary>
    [HttpPost("units/{unitId:int}/trade/smash-sell-horse")]
    public IActionResult UnitSmashSellHorse(int unitId, [FromBody] UnitSmashSellFoodRequest request)
        => ToActionResult(simulationHost.UnitSmashSellHorse(
            unitId, request.MinPriceMoneyPerGo, request.QuantityGo));

    /// <summary>据点市场快照（窗口 UI）。</summary>
    [HttpGet("strongholds/{strongholdId:int}/market")]
    public IActionResult GetMarketSnapshot(
        int strongholdId,
        [FromQuery] string commodity = "Food")
    {
        if (!Enum.TryParse<MarketCommodityType>(commodity, ignoreCase: true, out var parsed))
            return BadRequest(new { error = "InvalidCommodity" });

        var result = simulationHost.GetMarketSnapshot(strongholdId, parsed);
        if (result.IsSuccess && result.Value is not null)
        {
            logger.LogInformation(
                "MarketSnapshot {Summary}",
                MarketSnapshotDiagnostics.FormatSummary(result.Value));
        }

        return ToMarketSnapshotResult(result);
    }

    /// <summary>当主以官府库市价购粮。</summary>
    [HttpPost("strongholds/{strongholdId:int}/trade/smash-buy-food")]
    public IActionResult StrongholdLordSmashBuyFood(
        int strongholdId,
        [FromBody] UnitSmashBuyFoodRequest request)
        => ToActionResult(simulationHost.StrongholdLordSmashBuyFood(
            strongholdId, request.MaxPriceMoneyPerGo, request.QuantityGo));

    /// <summary>当主以官府库市价卖粮。</summary>
    [HttpPost("strongholds/{strongholdId:int}/trade/smash-sell-food")]
    public IActionResult StrongholdLordSmashSellFood(
        int strongholdId,
        [FromBody] UnitSmashSellFoodRequest request)
        => ToActionResult(simulationHost.StrongholdLordSmashSellFood(
            strongholdId, request.MinPriceMoneyPerGo, request.QuantityGo));

    /// <summary>当主以官府库市价买马。</summary>
    [HttpPost("strongholds/{strongholdId:int}/trade/smash-buy-horse")]
    public IActionResult StrongholdLordSmashBuyHorse(
        int strongholdId,
        [FromBody] UnitSmashBuyFoodRequest request)
        => ToActionResult(simulationHost.StrongholdLordSmashBuyHorse(
            strongholdId, request.MaxPriceMoneyPerGo, request.QuantityGo));

    /// <summary>当主以官府库市价卖马。</summary>
    [HttpPost("strongholds/{strongholdId:int}/trade/smash-sell-horse")]
    public IActionResult StrongholdLordSmashSellHorse(
        int strongholdId,
        [FromBody] UnitSmashSellFoodRequest request)
        => ToActionResult(simulationHost.StrongholdLordSmashSellHorse(
            strongholdId, request.MinPriceMoneyPerGo, request.QuantityGo));

    /// <summary>当主撤销官府挂单。</summary>
    [HttpPost("strongholds/{strongholdId:int}/trade/cancel-order")]
    public IActionResult StrongholdLordCancelMarketOrder(
        int strongholdId,
        [FromBody] CancelMarketOrderRequest request)
    {
        if (!Enum.TryParse<MarketCommodityType>(request.Commodity, ignoreCase: true, out var commodity))
            return BadRequest(new { error = "InvalidCommodity" });

        return ToActionResult(simulationHost.StrongholdLordCancelMarketOrder(
            strongholdId,
            request.OrderId,
            commodity));
    }

    /// <summary>设置 Unit 贸易策略。</summary>
    [HttpPost("units/{unitId:int}/trade/policy")]
    public IActionResult SetUnitTradePolicy(int unitId, [FromBody] SetUnitTradePolicyRequest request)
        => ToActionResult(simulationHost.SetUnitTradePolicy(
            unitId,
            Enum.Parse<UnitTradePolicy>(request.Policy, ignoreCase: true),
            request.LimitPriceMoneyPerGo,
            request.QuantityGo));

    /// <summary>推进 1 天。</summary>
    [HttpPost("advance-day")]
    public IActionResult AdvanceDay()
        => ToAdvanceDayResult(simulationHost.AdvanceDay());

    /// <summary>批量推进 1–31 日；仅构建一次最终世界 DTO。</summary>
    [HttpPost("advance-days")]
    public IActionResult AdvanceDays([FromBody] AdvanceDaysRequest request)
        => ToAdvanceDayResult(simulationHost.AdvanceDays(request.Days));

    /// <summary>登记谍报成果（约 2 个月后过期；开发/任务用）。</summary>
    [HttpPost("espionage-intel")]
    public IActionResult RecordEspionageIntel([FromBody] RecordEspionageIntelRequest request)
        => ToActionResult(simulationHost.RecordEspionageIntel(
            request.TargetKind,
            request.TargetId,
            request.Scope,
            request.Precision));

    /// <summary>调整据点税率；当主须在居城，仅直辖城可调整。</summary>
    [HttpPost("strongholds/{strongholdId:int}/set-tax-rate")]
    public IActionResult SetStrongholdTaxRates(int strongholdId, [FromBody] SetStrongholdTaxRateRequest request)
        => ToPolicyResult(simulationHost.SetStrongholdTaxRates(
            strongholdId,
            request.PollTaxRate,
            request.AgricultureTaxRate,
            request.CommerceTaxRate,
            request.TariffTaxRate));

    /// <summary>设置据点政务方针（军事优先 / 内政优先）。</summary>
    [HttpPost("strongholds/{strongholdId:int}/governance-priority")]
    public IActionResult SetStrongholdGovernancePriority(
        int strongholdId,
        [FromBody] SetStrongholdGovernancePriorityRequest request)
    {
        if (!Enum.TryParse<StrongholdGovernancePriority>(request.Priority, ignoreCase: true, out var priority))
            return BadRequest(new { error = "InvalidGovernancePriority" });

        return ToPolicyResult(simulationHost.SetStrongholdGovernancePriority(strongholdId, priority));
    }

    /// <summary>据点征兵：指派将领执行征兵任务。</summary>
    [HttpPost("strongholds/{strongholdId:int}/recruit")]
    public IActionResult RecruitAtStronghold(int strongholdId, [FromBody] RecruitAtStrongholdRequest request)
        => ToActionResult(simulationHost.RecruitAtStronghold(strongholdId, request.CharacterId));

    /// <summary>据点募兵：指派将领并设定预算。</summary>
    [HttpPost("strongholds/{strongholdId:int}/mercenary-recruit")]
    public IActionResult MercenaryRecruitAtStronghold(
        int strongholdId,
        [FromBody] MercenaryRecruitAtStrongholdRequest request)
        => ToActionResult(simulationHost.MercenaryRecruitAtStronghold(
            strongholdId,
            request.CharacterId,
            request.BudgetMoney));

    /// <summary>角色个人征兵：领主/代官/当主在城内亲自执行。</summary>
    [HttpPost("characters/{characterId:int}/personal-recruit")]
    public IActionResult PersonalRecruit(int characterId)
        => ToActionResult(simulationHost.PersonalRecruit(characterId));

    /// <summary>角色个人募兵：预算从执行者个人金库扣除。</summary>
    [HttpPost("characters/{characterId:int}/personal-mercenary-recruit")]
    public IActionResult PersonalMercenaryRecruit(
        int characterId,
        [FromBody] PersonalMercenaryRecruitRequest request)
        => ToActionResult(simulationHost.PersonalMercenaryRecruit(characterId, request.BudgetMoney));

    /// <summary>任命据点领主/代官；领主任命中当主 Id 表示设为直辖。</summary>
    [HttpPost("strongholds/{strongholdId:int}/appoint-lord")]
    public IActionResult AppointStrongholdLord(int strongholdId, [FromBody] AppointStrongholdLordRequest request)
        => ToActionResult(simulationHost.AppointStrongholdLord(
            strongholdId,
            request.CharacterId,
            request.AppointType));

    /// <summary>将领调动：自本据点派遣或自其它据点召集至本据点。</summary>
    [HttpPost("strongholds/{strongholdId:int}/transfer-character")]
    public IActionResult TransferCharacterToStronghold(
        int strongholdId,
        [FromBody] TransferCharacterRequest request)
        => ToActionResult(simulationHost.TransferCharacterToStronghold(
            strongholdId,
            request.CharacterId,
            request.Mode,
            request.DestinationStrongholdId));

    /// <summary>召回外派任务的将领。</summary>
    [HttpPost("strongholds/{strongholdId:int}/recall-character")]
    public IActionResult RecallCharacter(
        int strongholdId,
        [FromBody] RecallCharacterRequest request)
        => ToPolicyResult(simulationHost.RecallCharacter(strongholdId, request.CharacterId));

    /// <summary>外交：宣战/议和/同盟。</summary>
    [HttpPost("diplomacy/relation")]
    public IActionResult SetDiplomacyRelation([FromBody] SetDiplomacyRelationRequest request)
        => ToActionResult(simulationHost.SetDiplomacyRelation(
            request.TargetForceId,
            request.Relation));

    /// <summary>外交：支配/从属/释放/独立。</summary>
    [HttpPost("diplomacy/vassalage")]
    public IActionResult OrderDiplomacyVassalage([FromBody] DiplomacyVassalageRequest request)
        => ToActionResult(simulationHost.OrderDiplomacyVassalage(
            request.TargetForceId,
            request.Action));

    /// <summary>外政：任命/撤销内藩。</summary>
    [HttpPost("realm/inner-vassal")]
    public IActionResult OrderRealmInnerVassal([FromBody] RealmInnerVassalRequest request)
        => ToActionResult(simulationHost.OrderRealmInnerVassal(
            request.TargetForceId,
            request.Action));

    /// <summary>外交：预览使节任务成功率与行程。</summary>
    [HttpPost("diplomacy/mission/preview")]
    public IActionResult PreviewDiplomacyMission([FromBody] DiplomacyMissionPreviewRequest request)
        => ToPreviewResult(simulationHost.PreviewDiplomacyMission(
            request.CharacterId,
            request.TargetForceId,
            request.Action));

    /// <summary>外交：派遣使节任务。</summary>
    [HttpPost("diplomacy/mission")]
    public IActionResult OrderDiplomacyMission([FromBody] DiplomacyMissionOrderRequest request)
        => ToActionResult(simulationHost.OrderDiplomacyMission(
            request.CharacterId,
            request.TargetForceId,
            request.Action));

    /// <summary>外交：预览多条款和谈的战争分数成本与接受率。</summary>
    [HttpPost("diplomacy/peace/preview")]
    public IActionResult PreviewPeaceSettlement([FromBody] PeaceSettlementRequest request)
        => ToPreviewResult(simulationHost.PreviewPeaceSettlement(
            request.CharacterId,
            request.TargetForceId,
            new StrategyPeaceTermsDto
            {
                CededStrongholdIds = request.CededStrongholdIds,
                ReparationsMoney = request.ReparationsMoney,
                DemandOuterVassalage = request.DemandOuterVassalage,
            }));

    /// <summary>外交：派遣携带多条款和谈书的使节。</summary>
    [HttpPost("diplomacy/peace")]
    public IActionResult OrderPeaceSettlement([FromBody] PeaceSettlementRequest request)
        => ToActionResult(simulationHost.OrderPeaceSettlement(
            request.CharacterId,
            request.TargetForceId,
            new StrategyPeaceTermsDto
            {
                CededStrongholdIds = request.CededStrongholdIds,
                ReparationsMoney = request.ReparationsMoney,
                DemandOuterVassalage = request.DemandOuterVassalage,
            }));

    /// <summary>获取当前世界状态。</summary>
    [HttpGet("state")]
    public IActionResult GetState()
        => ToActionResult(simulationHost.GetState());

    /// <summary>获取当前剧本地图静态主数据（前端启动时加载一次）。</summary>
    [HttpGet("map")]
    public IActionResult GetMapMaster()
    {
        var result = simulationHost.GetMapMaster();
        if (result.IsSuccess)
            return Ok(result.Value);

        return BadRequest(new ApiErrorResponse(result.Error?.Code ?? "Unknown"));
    }

    /// <summary>导出当前仿真 JSON 存档。</summary>
    [HttpGet("save")]
    public IActionResult ExportSave()
    {
        var result = simulationHost.CaptureSave();
        if (!result.IsSuccess)
            return BadRequest(new ApiErrorResponse(result.Error?.Code ?? "Unknown"));

        return Ok(new StrategySaveExportResponse
        {
            Json = StrategySimulationHost.SerializeSave(result.Value!)
        });
    }

    /// <summary>从 JSON 存档恢复仿真。</summary>
    [HttpPost("restore-save")]
    public IActionResult RestoreSave([FromBody] StrategyRestoreSaveRequest request)
    {
        var parsed = StrategySimulationHost.DeserializeSave(request.Json);
        if (!parsed.IsSuccess)
            return BadRequest(new ApiErrorResponse(parsed.Error?.Code ?? "Unknown"));

        return ToActionResult(simulationHost.RestoreSave(parsed.Value!));
    }

    /// <summary>列出 10 个存档位摘要。</summary>
    [HttpGet("save-slots")]
    public IActionResult ListSaveSlots()
        => Ok(new StrategySaveSlotListResponse
        {
            Slots = saveSlotRepository.ListSlots().Select(MapSaveSlotSummary).ToList()
        });

    /// <summary>将当前仿真写入指定存档位（1–10）。</summary>
    [HttpPut("save-slots/{slot:int}")]
    public IActionResult SaveToSlot(int slot)
    {
        if (!IsValidSaveSlot(slot))
            return BadRequest(new ApiErrorResponse("InvalidSaveSlot"));

        var capture = simulationHost.CaptureSave();
        if (!capture.IsSuccess)
            return BadRequest(new ApiErrorResponse(capture.Error?.Code ?? "Unknown"));

        var summary = saveSlotRepository.WriteSlot(slot, new StrategySaveSlotEnvelope
        {
            SavedAtUtc = DateTime.UtcNow,
            LordName = simulationHost.LordName ?? "当主",
            Save = capture.Value!
        });

        return Ok(new StrategySaveSlotWriteResponse { Slot = MapSaveSlotSummary(summary) });
    }

    /// <summary>从指定存档位恢复仿真。</summary>
    [HttpPost("save-slots/{slot:int}/load")]
    public IActionResult LoadFromSlot(int slot)
    {
        if (!IsValidSaveSlot(slot))
            return BadRequest(new ApiErrorResponse("InvalidSaveSlot"));

        var envelope = saveSlotRepository.ReadEnvelope(slot);
        if (envelope?.Save is null)
            return BadRequest(new ApiErrorResponse("SaveSlotEmpty"));

        return ToActionResult(simulationHost.RestoreSave(envelope.Save));
    }

    /// <summary>获取移动诊断追踪（开发联调用）。</summary>
    [HttpGet("debug/movement-trace")]
    public IActionResult GetMovementTrace()
        => Ok(simulationHost.GetMovementTrace().Select(e => new StrategyMovementTraceEntryDto
        {
            Sequence = e.Sequence,
            At = e.At.ToString("O"),
            Phase = e.Phase,
            Message = e.Message,
            UnitId = e.UnitId,
            FromX = e.From?.X,
            FromY = e.From?.Y,
            ToX = e.To?.X,
            ToY = e.To?.Y,
            Detail = e.Detail
        }));

    /// <summary>获取 AI 决策思维链（开发联调用）。</summary>
    [HttpGet("debug/ai-decision-trace")]
    public IActionResult GetAiDecisionTrace()
        => Ok(simulationHost.GetAiDecisionTrace().Select(e => new StrategyAiDecisionTraceEntryDto
        {
            Sequence = e.Sequence,
            At = e.At.ToString("O"),
            Phase = e.Phase,
            Code = e.Code,
            Message = e.Message,
            UnitId = e.UnitId,
            UnitName = e.UnitName,
            ForceId = e.ForceId,
            ActedOrChanged = e.ActedOrChanged,
            FromDirective = e.FromDirective,
            ToDirective = e.ToDirective,
            CurrentDirective = e.CurrentDirective,
            TargetUnitId = e.TargetUnitId,
            TargetStrongholdId = e.TargetStrongholdId,
            TargetX = e.TargetPoint?.X,
            TargetY = e.TargetPoint?.Y,
            Stance = e.Stance,
            SiegeMode = e.SiegeMode,
            UnitStatus = e.UnitStatus,
            Steps = e.Steps
        }));

    /// <summary>获取日推进 debug 日志（内存快照 + 最近写入文件路径）。</summary>
    [HttpGet("debug/day-log")]
    public IActionResult GetDayDebugLog()
        => Ok(simulationHost.GetDayDebugLog());

    private IActionResult ToActionResult(GameResult<StrategyWorldStateDto> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return BadRequest(new ApiErrorResponse(result.Error?.Code ?? "Unknown"));
    }

    private IActionResult ToPreviewResult(GameResult<StrategyPathPreviewDto> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return BadRequest(new ApiErrorResponse(result.Error?.Code ?? "Unknown"));
    }

    private IActionResult ToPreviewResult(GameResult<StrategyDiplomacyMissionPreviewDto> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return BadRequest(new ApiErrorResponse(result.Error?.Code ?? "Unknown"));
    }

    private IActionResult ToPreviewResult(GameResult<StrategyPeaceSettlementPreviewDto> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return BadRequest(new ApiErrorResponse(result.Error?.Code ?? "Unknown"));
    }

    private IActionResult ToMarketSnapshotResult(GameResult<StrategyMarketSnapshotDto> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return BadRequest(new ApiErrorResponse(result.Error?.Code ?? "Unknown"));
    }

    private IActionResult ToBattlePreviewResult(GameResult<StrategyBattlePreviewDto> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return BadRequest(new ApiErrorResponse(result.Error?.Code ?? "Unknown"));
    }

    private IActionResult ToBattleResult(GameResult<StrategyInstantBattleResponseDto> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return BadRequest(new ApiErrorResponse(result.Error?.Code ?? "Unknown"));
    }

    private IActionResult ToPolicyResult(GameResult<StrategyPolicyChangeResponseDto> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return BadRequest(new ApiErrorResponse(result.Error?.Code ?? "Unknown"));
    }

    private IActionResult ToAdvanceDayResult(GameResult<StrategyAdvanceDayResponseDto> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return BadRequest(new ApiErrorResponse(result.Error?.Code ?? "Unknown"));
    }

    private static IReadOnlyList<Point2>? ToViaPoints(IReadOnlyList<MapPointRequest>? via)
        => via?.Select(p => new Point2(p.X, p.Y)).ToList();

    private static bool IsValidSaveSlot(int slot)
        => slot is >= 1 and <= StrategySaveSlotRepository.MaxSlots;

    private static StrategySaveSlotSummaryDto MapSaveSlotSummary(StrategySaveSlotSummary summary)
        => new()
        {
            Slot = summary.Slot,
            Occupied = summary.Occupied,
            SavedAtUtc = summary.SavedAtUtc?.ToString("O"),
            ScenarioId = summary.ScenarioId,
            LordName = summary.LordName,
            DateLabel = summary.DateLabel
        };
}
