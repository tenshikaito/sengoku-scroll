using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Battle;

/// <summary>战后士气修正与低士气禁战。</summary>
public static class BattleMoraleRules
{
    /// <summary>按胜负与是否大胜调整双方士气，并触发撤退/振奋/恐惧状态。</summary>
    public static void ApplyBattleOutcome(Unit winner, Unit loser, bool decisiveVictory)
    {
        // 业务：大胜全额士气变动，小胜减半
        var winGain = decisiveVictory ? BattleConstants.WinnerMoraleGain : BattleConstants.WinnerMoraleGain / 2;
        var lossDrop = decisiveVictory ? BattleConstants.LoserMoraleLoss : BattleConstants.LoserMoraleLoss / 2;

        winner.Morale = (byte)Math.Clamp(winner.Morale + winGain, 0, 100);
        loser.Morale = (byte)Math.Clamp(loser.Morale - lossDrop, 0, 100);

        // 业务：败方低士气自动改撤退方针并清空攻击目标
        if (loser.Morale < BattleConstants.LowMoraleEngageThreshold && loser.Soldier > 0)
        {
            loser.Directive = UnitDirective.Retreat;
            loser.Stance = UnitStance.Normal;
            loser.ActionTarget.UnitId = 0;
        }

        // 业务：胜方高士气且待命时进入振奋状态
        if (winner.Morale >= BattleConstants.InspiringMoraleThreshold && winner.Status == UnitStatus.Waiting)
            winner.Status = UnitStatus.Inspiring;

        // 业务：败方极低士气进入恐惧状态
        if (loser.Morale < BattleConstants.FearfulMoraleThreshold && loser.Status != UnitStatus.Chaos)
            loser.Status = UnitStatus.Fearful;
    }
}
