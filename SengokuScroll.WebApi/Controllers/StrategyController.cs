using Microsoft.AspNetCore.Mvc;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.WebApi.Models;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.WebApi.Controllers;

/// <summary>策略模式 API（M2-a）：命令成功后统一返回 <see cref="StrategyWorldStateDto"/>。</summary>
[ApiController]
[Route("strategy")]
[Route("api/strategy")]
public class StrategyController(StrategySimulationHost simulationHost) : ControllerBase
{
    /// <summary>加载 JSON 剧本（如 mini_kanto）。</summary>
    [HttpPost("load")]
    public IActionResult Load([FromBody] LoadScenarioRequest request)
        => ToActionResult(simulationHost.LoadScenario(request.ScenarioId));

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
            request.Money));

    /// <summary>推进 1 天。</summary>
    [HttpPost("advance-day")]
    public IActionResult AdvanceDay()
        => ToAdvanceDayResult(simulationHost.AdvanceDay());

    /// <summary>获取当前世界状态。</summary>
    [HttpGet("state")]
    public IActionResult GetState()
        => ToActionResult(simulationHost.GetState());

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
}
