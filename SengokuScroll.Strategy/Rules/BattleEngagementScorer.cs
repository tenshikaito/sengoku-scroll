using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Constants;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>将全因素战斗评估接入战略 AI：目标评分、接敌意愿、附近威胁。</summary>
public static class BattleEngagementScorer
{
    /// <summary>以 self 为攻方评估对 enemy 的强袭胜率（0–100）。</summary>
    public static int ScoreCommitWinRate(
        Unit self,
        Unit enemy,
        GameData gameData,
        GameMapMasterData? mapMaster = null,
        int standoffDays = 0)
    {
        if (!BattleFactorEvaluator.CanUnitEngage(self))
            return 0;

        var ctx = new BattleEvaluationContext
        {
            Attacker = self,
            Defender = enemy,
            GameData = gameData,
            MapMaster = mapMaster,
            Phase = BattleEvaluationPhase.Commit,
            StandoffDays = standoffDays,
            EngagementKind = BattleEngagementClassifier.Classify(self, enemy, gameData)
        };

        return BattleFactorEvaluator.ComputeAdjustedCommitWinRate(ctx, selfIsAttacker: true);
    }

    /// <summary>
    /// 附近威胁分：距离越近、敌军越强、正在攻击己方/友军时越高。
    /// 用于优先清剿邻敌，避免被偷袭。
    /// </summary>
    public static int ScoreNearbyThreat(Unit self, Unit enemy, GameData gameData)
    {
        var dist = Manhattan(self.Location, enemy.Location);
        if (dist <= 0)
            return 0;

        var score = 0;

        // 业务：邻接威胁最高；2 格内次之（防侧翼偷袭）
        score += dist switch
        {
            1 => 800,
            2 => 420,
            3 => 180,
            _ => Math.Max(0, 120 - dist * 25)
        };

        // 业务：兵力威胁：敌军相对己方越强，越应优先处理
        if (self.Soldier > 0)
        {
            var ratio = (double)enemy.Soldier / self.Soldier;
            score += (int)Math.Round(Math.Clamp(ratio, 0.2, 3.0) * 120);
        }
        else
        {
            score += enemy.Soldier / 10;
        }

        // 业务：正在攻击己方或友军 → 紧急
        if (enemy.Stance == UnitStance.Attacking && enemy.ActionTarget.UnitId > 0)
        {
            if (enemy.ActionTarget.UnitId == self.Id)
                score += 350;
            else if (gameData.Units.TryGetValue(enemy.ActionTarget.UnitId, out var target)
                     && target.ForceId == self.ForceId)
                score += 220;
        }

        // 业务：进攻方针敌军更危险
        if (MoveEngagementRules.IsAggressiveDirective(enemy.Directive))
            score += 80;

        // 业务：己方附近友军少、敌军多 → 更应先清邻敌
        var nearbyEnemies = CountNearbyHostiles(self, gameData, radius: 2);
        if (nearbyEnemies >= 2)
            score += 60 * (nearbyEnemies - 1);

        return score;
    }

    /// <summary>综合威胁与胜率的进攻目标分（越高越优先）。</summary>
    public static int ScoreAttackTarget(
        Unit self,
        Unit enemy,
        GameData gameData,
        GameMapMasterData? mapMaster = null)
    {
        var threat = ScoreNearbyThreat(self, enemy, gameData);
        var winRate = ScoreCommitWinRate(self, enemy, gameData, mapMaster);
        var dist = Manhattan(self.Location, enemy.Location);

        // 业务：胜率高可打；胜率过低则大幅降权（仍保留近距威胁权重，由方针层决定是否撤退）
        var winWeight = winRate >= BattleConstants.CommitAssaultWinRateThreshold
            ? winRate * 4
            : winRate * 2;

        return threat + winWeight - dist * 15;
    }

    /// <summary>统计指定半径内敌对军事单位数量。</summary>
    public static int CountNearbyHostiles(Unit unit, GameData gameData, int radius)
    {
        var count = 0;
        foreach (var other in ResolveHostileUnits(unit, gameData))
        {
            if (Manhattan(unit.Location, other.Location) <= radius)
                count++;
        }

        return count;
    }

    private static IEnumerable<Unit> ResolveHostileUnits(Unit unit, GameData gameData)
        => StrategyUnitAIRules.ResolveHostileUnits(unit, gameData);

    private static int Manhattan(Common.Types.Point3 a, Common.Types.Point3 b)
        => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
}
