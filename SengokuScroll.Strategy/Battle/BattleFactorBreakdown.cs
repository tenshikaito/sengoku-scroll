namespace SengokuScroll.Strategy.Battle;

/// <summary>各因素汇总后的战斗修正（可写入战报/调试）。</summary>
public sealed class BattleFactorBreakdown
{
    /// <summary>攻方胜率累计修正（百分点）。</summary>
    public int AttackerWinRateDelta { get; set; }

    /// <summary>守方胜率累计修正（百分点）。</summary>
    public int DefenderWinRateDelta { get; set; }

    /// <summary>攻方有效战力倍率。</summary>
    public double AttackerPowerScale { get; set; } = 1.0;

    /// <summary>守方有效战力倍率。</summary>
    public double DefenderPowerScale { get; set; } = 1.0;

    /// <summary>攻方伤亡倍率。</summary>
    public double AttackerCasualtyScale { get; set; } = 1.0;

    /// <summary>守方伤亡倍率。</summary>
    public double DefenderCasualtyScale { get; set; } = 1.0;

    /// <summary>低概率莽撞强袭（轻敌/复仇等）。</summary>
    public bool ForceCommit { get; set; }

    /// <summary>禁止强袭/接敌（低士气、混乱、撤退中等）。</summary>
    public bool BlockCommit { get; set; }

    /// <summary>胜方战后士气变动（默认 +12）。</summary>
    public int WinnerMoraleDelta { get; set; } = 12;

    /// <summary>败方战后士气变动（默认 -18）。</summary>
    public int LoserMoraleDelta { get; set; } = -18;

    public List<BattleFactorNote> Notes { get; } = [];

    /// <summary>攻方净胜率修正 = 攻方修正 − 守方修正。</summary>
    public int NetAttackerWinRateDelta => AttackerWinRateDelta - DefenderWinRateDelta;

    /// <summary>记录一条因素明细，供战报与调试展示。</summary>
    public void Add(string factorId, string label, int attackerDelta, int defenderDelta = 0, string? detail = null)
        => Notes.Add(new BattleFactorNote(factorId, label, attackerDelta, defenderDelta, detail));
}

public readonly record struct BattleFactorNote(
    /// <summary>因素标识，供调试与日志分类。</summary>
    string FactorId,
    /// <summary>因素中文标签。</summary>
    string Label,
    int AttackerWinRateDelta,
    int DefenderWinRateDelta,
    string? Detail);
