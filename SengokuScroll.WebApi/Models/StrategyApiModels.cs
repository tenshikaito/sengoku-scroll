using SengokuScroll.Strategy.Models;

namespace SengokuScroll.WebApi.Models;

/// <summary>加载剧本请求。</summary>
public sealed class LoadScenarioRequest
{
    public required string ScenarioId { get; init; }

    /// <summary>全势力交由 AI 控制；用于观战、长局验证与平衡测试。</summary>
    public bool AllForcesAiControlled { get; init; }

    /// <summary>Easy | Normal | Hard | Custom；省略则沿用剧本 JSON。</summary>
    public string? Difficulty { get; init; }

    /// <summary>Custom 难度下的开局选项。</summary>
    public GameStartOptionsDto? CustomStartOptions { get; init; }
}

/// <summary>地图格点。</summary>
public sealed class MapPointRequest
{
    public required int X { get; init; }

    public required int Y { get; init; }
}

/// <summary>单位移动目标格。</summary>
public sealed class MoveUnitRequest
{
    public required int X { get; init; }

    public required int Y { get; init; }

    /** 路径预览/移动时可选起点（缺省为单位当前位置）。 */
    public int? FromX { get; init; }

    public int? FromY { get; init; }

    /** 最终目标前的中继格（按顺序经过）。 */
    public List<MapPointRequest>? Via { get; init; }
}

/// <summary>角色入城目标据点。</summary>
public sealed class EnterStrongholdRequest
{
    public required int StrongholdId { get; init; }

    /// <summary>据点被封锁/包围时须为 true 以强行出入。</summary>
    public bool Force { get; init; }
}

/// <summary>角色出城（可选强行突围）。</summary>
public sealed class CharacterGateRequest
{
    public bool Force { get; init; }
}

/// <summary>单位方针变更（M3-b）。</summary>
public sealed class SetUnitDirectiveRequest
{
    /** UnitDirective 枚举名，如 Move / Occupy / Retreat。 */
    public required string Directive { get; init; }

    /** 指令下达方格（缺省为目标单位所在格，同格即时生效）。 */
    public int? IssuerX { get; init; }

    public int? IssuerY { get; init; }

    /** 信使出发据点 Id（缺省为势力默认据点）。 */
    public int? SourceStrongholdId { get; init; }
}

/// <summary>API 成功响应（无额外数据时）。</summary>
public sealed record ApiSuccessResponse(bool Success = true);

/// <summary>API 错误响应。</summary>
public sealed record ApiErrorResponse(string ErrorCode, bool Success = false);

/// <summary>存档 JSON 导出响应。</summary>
public sealed class StrategySaveExportResponse
{
    public required string Json { get; init; }
}

/// <summary>从 JSON 恢复存档请求。</summary>
public sealed class StrategyRestoreSaveRequest
{
    public required string Json { get; init; }
}

/// <summary>存档位列表响应。</summary>
public sealed class StrategySaveSlotListResponse
{
    public required IReadOnlyList<StrategySaveSlotSummaryDto> Slots { get; init; }
}

/// <summary>单个存档位摘要。</summary>
public sealed class StrategySaveSlotSummaryDto
{
    public required int Slot { get; init; }

    public required bool Occupied { get; init; }

    public string? SavedAtUtc { get; init; }

    public string? ScenarioId { get; init; }

    public string? LordName { get; init; }

    public string? DateLabel { get; init; }
}

/// <summary>写入存档位响应。</summary>
public sealed class StrategySaveSlotWriteResponse
{
    public required StrategySaveSlotSummaryDto Slot { get; init; }
}

/// <summary>攻城指令请求。</summary>
public sealed class SiegeOrderRequest
{
    public required int StrongholdId { get; init; }

    /** Assault | Encircle */
    public required string Mode { get; init; }
}

/// <summary>部队合并请求：将 source 并入 target。</summary>
public sealed class MergeUnitRequest
{
    public required int TargetUnitId { get; init; }
}

/// <summary>部队分兵请求。</summary>
public sealed class SplitUnitRequest
{
    public required IReadOnlyList<int> SubUnitIds { get; init; }

    public required int SpawnX { get; init; }

    public required int SpawnY { get; init; }

    public string? Name { get; init; }
}

/// <summary>出征编组条目。</summary>
public sealed class DeployCompositionRequestEntry
{
    public required int TypeId { get; init; }

    public string? TypeName { get; init; }

    public required int Soldiers { get; init; }

    public int? CommanderId { get; init; }
}

/// <summary>居城出征请求。</summary>
public sealed class DeployFromStrongholdRequest
{
    public string? UnitName { get; init; }

    public required int CommanderId { get; init; }

    public required IReadOnlyList<DeployCompositionRequestEntry> Composition { get; init; }

    public int? Food { get; init; }

    public int? Money { get; init; }

    /// <summary>false=组建后在城中（默认）；true=组建后立即出城占格。</summary>
    public bool DeployToMap { get; init; }
}

/// <summary>创立商店（无需许可）。</summary>
public sealed class CreateMerchantShopRequest
{
    public string? HouseName { get; init; }
}

/// <summary>Unit 市价购粮（砸单）。</summary>
public sealed class UnitSmashBuyFoodRequest
{
    public required int MaxPriceMoneyPerGo { get; init; }

    public int QuantityGo { get; init; }
}

/// <summary>Unit 市价卖粮（砸单）。</summary>
public sealed class UnitSmashSellFoodRequest
{
    public required int MinPriceMoneyPerGo { get; init; }

    public int QuantityGo { get; init; }
}

/// <summary>撤销官府挂单。</summary>
public sealed class CancelMarketOrderRequest
{
    public required int OrderId { get; init; }

    public string Commodity { get; init; } = "Food";
}

/// <summary>设置 Unit 贸易策略。</summary>
public sealed class SetUnitTradePolicyRequest
{
    /** None | WaitBuyFood | WaitSellFood */
    public required string Policy { get; init; }

    public required int LimitPriceMoneyPerGo { get; init; }

    public int QuantityGo { get; init; }
}

/// <summary>登记谍报成果（开发/任务用）。</summary>
public sealed class RecordEspionageIntelRequest
{
    /** Stronghold | Unit */
    public required string TargetKind { get; init; }

    public required int TargetId { get; init; }

    /** Military | Domestic | Both */
    public required string Scope { get; init; }

    /** Fuzzy | Exact */
    public required string Precision { get; init; }
}

/// <summary>调整据点税率；省略的字段保持不变。</summary>
public sealed class SetStrongholdTaxRateRequest
{
    public byte? PollTaxRate { get; init; }

    public byte? AgricultureTaxRate { get; init; }

    public byte? CommerceTaxRate { get; init; }

    public byte? TariffTaxRate { get; init; }
}

/// <summary>设置据点政务方针。</summary>
public sealed class SetStrongholdGovernancePriorityRequest
{
    /** Military | Domestic | Autonomous */
    public required string Priority { get; init; }
}

/// <summary>据点征兵请求：指派城内待命将领。</summary>
public sealed class RecruitAtStrongholdRequest
{
    public required int CharacterId { get; init; }
}

/// <summary>据点募兵请求：指派将领并设定预算。</summary>
public sealed class MercenaryRecruitAtStrongholdRequest
{
    public required int CharacterId { get; init; }

    public required int BudgetMoney { get; init; }
}

/// <summary>角色个人募兵请求：预算从执行者个人金库扣除。</summary>
public sealed class PersonalMercenaryRecruitRequest
{
    public required int BudgetMoney { get; init; }
}

/// <summary>任命据点领主或代官；领主任命时 characterId 为当主 Id 表示设为直辖。</summary>
public sealed class AppointStrongholdLordRequest
{
    public required int CharacterId { get; init; }

    /** Lord | Mayor */
    public string AppointType { get; init; } = "Lord";
}

/// <summary>将领调动：派遣或召集。</summary>
public sealed class TransferCharacterRequest
{
    public required int CharacterId { get; init; }

    /** Dispatch | Summon */
    public string Mode { get; init; } = "Summon";

    /** 派遣模式下的目标据点 Id。 */
    public int DestinationStrongholdId { get; init; }
}

/// <summary>召回外派任务的将领。</summary>
public sealed class RecallCharacterRequest
{
    public required int CharacterId { get; init; }
}

/// <summary>外交关系变更。</summary>
public sealed class SetDiplomacyRelationRequest
{
    public required int TargetForceId { get; init; }

    /** Neutral | Allied | Enemy */
    public required string Relation { get; init; }
}

/// <summary>藩属/独立指令。</summary>
public sealed class DiplomacyVassalageRequest
{
    public required int TargetForceId { get; init; }

    /** impose | submit | release | independence */
    public required string Action { get; init; }
}

/// <summary>内藩外政指令。</summary>
public sealed class RealmInnerVassalRequest
{
    public required int TargetForceId { get; init; }

    /** appoint | revoke */
    public required string Action { get; init; }
}

/// <summary>外交使节任务预览。</summary>
public sealed class DiplomacyMissionPreviewRequest
{
    public int CharacterId { get; init; }

    public required int TargetForceId { get; init; }

    /** Ally | War | Peace */
    public required string Action { get; init; }
}

/// <summary>外交使节任务下达。</summary>
public sealed class DiplomacyMissionOrderRequest
{
    public required int CharacterId { get; init; }

    public required int TargetForceId { get; init; }

    /** Ally | War | Peace */
    public required string Action { get; init; }
}

/// <summary>人物互动请求；Interaction 为 Talk 或 Gift。</summary>
public sealed class CharacterInteractionRequest
{
    public required int TargetCharacterId { get; init; }

    public required string Interaction { get; init; }
}

/// <summary>批量推进请求；试玩/观战模式用于减少 HTTP 与序列化开销。</summary>
public sealed class AdvanceDaysRequest
{
    public int Days { get; init; } = 1;
}

/// <summary>多条款和谈预览/下达。</summary>
public sealed class PeaceSettlementRequest
{
    public required int CharacterId { get; init; }

    public required int TargetForceId { get; init; }

    public IReadOnlyList<int> CededStrongholdIds { get; init; } = [];

    public int ReparationsMoney { get; init; }

    public bool DemandOuterVassalage { get; init; }
}
