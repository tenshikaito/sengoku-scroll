namespace SengokuScroll.WebApi.Models;

/// <summary>加载剧本请求。</summary>
public sealed class LoadScenarioRequest
{
    public required string ScenarioId { get; init; }
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
