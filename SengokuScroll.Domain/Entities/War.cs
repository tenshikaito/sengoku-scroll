using SengokuScroll.Domain.Types;

namespace SengokuScroll.Domain.Entities;

/// <summary>
/// 法理战争：宣战方为攻方；参战国分两侧；同一 Battlefield 只挂一场 War。
/// </summary>
public class War
{
    public int Id { get; set; }

    /// <summary>宣战方势力（WarAggressor）。</summary>
    public int AggressorForceId { get; set; }

    /// <summary>主守/被宣战方势力。</summary>
    public int DefenderForceId { get; set; }

    /// <summary>攻方参战国（含主战国）。</summary>
    public List<int> AggressorForceIds { get; set; } = [];

    /// <summary>守方参战国（含主守）。</summary>
    public List<int> DefenderForceIds { get; set; } = [];

    public GameDate StartDate { get; set; }

    public bool IsEnded { get; set; }

    public GameDate? EndDate { get; set; }
}
