using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Strategy.Battle;

/// <summary>阵型 Id → 攻防修正（无 MasterData 表时的内置集）。</summary>
public static class BattleFormationRules
{
    /// <summary>鱼鳞阵 Id——守方防御向。</summary>
    public const int FishScale = 1;
    /// <summary>鹤翼阵 Id——攻方展开向。</summary>
    public const int CraneWing = 2;
    /// <summary>偃月阵 Id——攻守均衡。</summary>
    public const int Crescent = 3;
    /// <summary>方阵 Id——稳健通用。</summary>
    public const int Square = 4;

    /// <summary>按阵型 Id 施加攻防战力倍率与胜率修正。</summary>
    public static void ApplyFormation(Unit unit, bool isAttacker, BattleFactorBreakdown b)
    {
        if (unit.FormationId <= 0)
            return;

        // 业务：鱼鳞守方 +10% 战力/+3% 胜率；鹤翼攻方 +10%/+3%；偃月双方 +6%/+2%
        var (powerScale, winDelta, label) = unit.FormationId switch
        {
            FishScale => (isAttacker ? 1.0 : 1.10, isAttacker ? 0 : 3, "鱼鳞阵"),
            CraneWing => (isAttacker ? 1.10 : 1.0, isAttacker ? 3 : 0, "鹤翼阵"),
            Crescent => (1.06, isAttacker ? 2 : 2, "偃月阵"),
            Square => (1.04, isAttacker ? 1 : 1, "方阵"),
            _ => (1.0, 0, $"阵型#{unit.FormationId}")
        };

        if (isAttacker)
        {
            b.AttackerPowerScale *= powerScale;
            b.AttackerWinRateDelta += winDelta;
        }
        else
        {
            b.DefenderPowerScale *= powerScale;
            b.DefenderWinRateDelta += winDelta;
        }

        if (powerScale != 1.0 || winDelta != 0)
            b.Add("formation", label, isAttacker ? winDelta : 0, isAttacker ? 0 : winDelta, unit.Name);
    }
}
