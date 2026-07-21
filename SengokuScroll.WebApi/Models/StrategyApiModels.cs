using SengokuScroll.Strategy.Models;

namespace SengokuScroll.WebApi.Models;

/// <summary>加载剧本请求。</summary>
public sealed class LoadScenarioRequest
{
    public required string ScenarioId { get; init; }

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
