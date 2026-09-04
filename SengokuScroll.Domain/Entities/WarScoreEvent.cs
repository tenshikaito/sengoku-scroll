using SengokuScroll.Domain.Types;

namespace SengokuScroll.Domain.Entities;

/// <summary>单次战争分数变化；Delta 始终以宣战方视角记录。</summary>
public sealed class WarScoreEvent
{
    public GameDate Date { get; set; }

    public int Delta { get; set; }

    /// <summary>BattleVictory | StrongholdOccupied | PeaceSettlement。</summary>
    public required string Reason { get; set; }

    public int ActingForceId { get; set; }

    public int TargetForceId { get; set; }

    public int? SourceEntityId { get; set; }

    public string? Description { get; set; }
}
