using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Strategy.Battle;

/// <summary>自动战斗评估阶段。</summary>
public enum BattleEvaluationPhase
{
    /// <summary>是否接敌 / 进入战斗事件。</summary>
    Engagement,

    /// <summary>对峙日是否强袭（Commit）。</summary>
    Commit,

    /// <summary>纠缠决战胜负与伤亡。</summary>
    Resolve,

    /// <summary>战后士气、方针、追击。</summary>
    Aftermath
}

/// <summary>单次战斗评估的输入上下文（攻/守角色已确定）。</summary>
public sealed class BattleEvaluationContext
{
    public required Unit Attacker { get; init; }

    public required Unit Defender { get; init; }

    public required GameData GameData { get; init; }

    public GameMapMasterData? MapMaster { get; init; }

    /// <summary>当前评估所处阶段（接敌/强袭/决战/战后）。</summary>
    public BattleEvaluationPhase Phase { get; init; }

    /// <summary>当前对峙累计日数（Commit/Resolve 用）。</summary>
    public int StandoffDays { get; init; }

    /// <summary>接敌类型（野战/伏击/攻城）。</summary>
    public BattleEngagementKind EngagementKind { get; init; } = BattleEngagementKind.FieldBattle;

    /// <summary>攻方主将（由 LeaderId 解析）。</summary>
    public Character? AttackerCommander => ResolveCommander(Attacker);

    /// <summary>守方主将（由 LeaderId 解析）。</summary>
    public Character? DefenderCommander => ResolveCommander(Defender);

    private Character? ResolveCommander(Unit unit)
        => unit.LeaderId > 0 && GameData.Characters.TryGetValue(unit.LeaderId, out var c) ? c : null;
}
