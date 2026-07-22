using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Definitions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Constants;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Battle;

/// <summary>
/// 自动战斗全因素评估器：将游戏模型中的属性/状态映射为胜率、战力、伤亡与 Commit 修正。
/// </summary>
public static class BattleFactorEvaluator
{
    /// <summary>汇总全部战斗因素，生成战力、胜率、伤亡与 Commit 门禁修正。</summary>
    public static BattleFactorBreakdown Evaluate(BattleEvaluationContext ctx)
    {
        var breakdown = new BattleFactorBreakdown();

        ApplyUnitCoreFactors(ctx, breakdown);
        ApplyCompositionFactors(ctx, breakdown);
        ApplyCommanderFactors(ctx, breakdown);
        BattleFormationRules.ApplyFormation(ctx.Attacker, isAttacker: true, breakdown);
        BattleFormationRules.ApplyFormation(ctx.Defender, isAttacker: false, breakdown);
        BattleEquipmentRules.ApplyCommanderEquipment(ctx.AttackerCommander, isAttacker: true, breakdown);
        BattleEquipmentRules.ApplyCommanderEquipment(ctx.DefenderCommander, isAttacker: false, breakdown);
        ApplyPostureFactors(ctx, breakdown);
        BattleDirectiveRules.ApplyCombatDirectives(ctx, breakdown);
        BattleEngagementClassifier.ApplyEngagementKind(
            ctx.EngagementKind, breakdown, ctx.Defender, ctx.GameData);
        ApplySupplyFactors(ctx, breakdown);
        ApplyTerrainFactors(ctx, breakdown);
        ApplyWeatherFactors(ctx, breakdown);
        ApplyStratagemFactors(ctx, breakdown);
        ApplyStandoffIntelFactors(ctx, breakdown);
        ApplyReinforcementFactors(ctx, breakdown);
        ApplyPhaseGates(ctx, breakdown);

        return breakdown;
    }

    /// <summary>计算攻方胜率（5%～95%）：有效战力比 + 净因素修正。</summary>
    public static int ComputeAttackerWinRatePercent(BattleEvaluationContext ctx)
    {
        var breakdown = Evaluate(ctx);
        var defenderTerrain = ResolveDefenderTerrain(ctx);
        var atkPower = Math.Max(0, (int)Math.Round(
            BattleCompositionCalculator.ComputeEffectivePower(ctx.Attacker, ctx.GameData, defenderTerrain)
            * breakdown.AttackerPowerScale));
        var defPower = Math.Max(0, (int)Math.Round(
            BattleCompositionCalculator.ComputeEffectivePower(ctx.Defender, ctx.GameData, defenderTerrain)
            * breakdown.DefenderPowerScale));

        if (atkPower + defPower == 0)
            return 50;

        // 业务：基础胜率 = 攻方战力 / 双方战力之和
        var baseRate = (int)Math.Round((double)atkPower / (atkPower + defPower) * 100);
        var adjusted = baseRate + breakdown.NetAttackerWinRateDelta;

        return (int)Math.Clamp(
            adjusted,
            BattleConstants.MinWinRatePercent,
            BattleConstants.MaxWinRatePercent);
    }

    /// <summary>以己方视角重算强袭胜率（交换攻守角色后取攻方胜率）。</summary>
    public static int ComputeAdjustedCommitWinRate(
        BattleEvaluationContext ctx,
        bool selfIsAttacker)
    {
        var self = selfIsAttacker ? ctx.Attacker : ctx.Defender;
        var enemy = selfIsAttacker ? ctx.Defender : ctx.Attacker;
        var asAttackerCtx = new BattleEvaluationContext
        {
            Attacker = self,
            Defender = enemy,
            GameData = ctx.GameData,
            MapMaster = ctx.MapMaster,
            Phase = BattleEvaluationPhase.Commit,
            StandoffDays = ctx.StandoffDays
        };

        return ComputeAttackerWinRatePercent(asAttackerCtx);
    }

    /// <summary>判断单位是否具备接敌/强袭资格（有兵、非混乱、非撤退、士气达标）。</summary>
    public static bool CanUnitEngage(Unit unit)
    {
        if (unit.Soldier <= 0 || unit.Status == UnitStatus.Chaos)
            return false;

        if (unit.Directive == UnitDirective.Retreat)
            return false;

        return unit.Morale >= BattleConstants.LowMoraleEngageThreshold;
    }

    /// <summary>单位核心属性：士气、训练、疲劳。</summary>
    private static void ApplyUnitCoreFactors(BattleEvaluationContext ctx, BattleFactorBreakdown b)
    {
        ApplyMorale(ctx.Attacker, isAttacker: true, b);
        ApplyMorale(ctx.Defender, isAttacker: false, b);
        ApplyTraining(ctx.Attacker, isAttacker: true, b);
        ApplyTraining(ctx.Defender, isAttacker: false, b);
        ApplyTiredness(ctx.Attacker, isAttacker: true, b);
        ApplyTiredness(ctx.Defender, isAttacker: false, b);
    }

    /// <summary>士气偏离 50 每 5 点修正 1% 胜率；低士气禁止强袭。</summary>
    private static void ApplyMorale(Unit unit, bool isAttacker, BattleFactorBreakdown b)
    {
        // 业务：士气 50 为基准，每偏离 5 点 ±1% 胜率
        var delta = (unit.Morale - 50) / 5;
        if (delta != 0)
        {
            if (isAttacker)
                b.AttackerWinRateDelta += delta;
            else
                b.DefenderWinRateDelta += delta;
        }

        if (unit.Morale < BattleConstants.LowMoraleEngageThreshold)
            b.BlockCommit = true;
    }

    /// <summary>训练度线性映射战力倍率：0 训练 ×0.72，满训练 ×1.28。</summary>
    private static void ApplyTraining(Unit unit, bool isAttacker, BattleFactorBreakdown b)
    {
        // 业务：训练 0→0.72，训练 100→1.28
        var scale = 0.72 + unit.Training / 100.0 * 0.56;
        if (isAttacker)
            b.AttackerPowerScale *= scale;
        else
            b.DefenderPowerScale *= scale;
    }

    /// <summary>疲劳每 10 点扣 1% 胜率，上限 -15%。</summary>
    private static void ApplyTiredness(Unit unit, bool isAttacker, BattleFactorBreakdown b)
    {
        if (unit.Tiredness <= 0)
            return;

        // 业务：疲劳惩罚 = min(15, 疲劳/10)
        var penalty = Math.Min(15, unit.Tiredness / 10);
        if (isAttacker)
            b.AttackerWinRateDelta -= penalty;
        else
            b.DefenderWinRateDelta -= penalty;
    }

    /// <summary>将领统率/武力、伤病、智谋对比与军事熟练度。</summary>
    private static void ApplyCommanderFactors(BattleEvaluationContext ctx, BattleFactorBreakdown b)
    {
        ApplyCommanderStats(ctx.AttackerCommander, ctx.DefenderCommander, isAttackerSide: true, b);
        ApplyCommanderStats(ctx.DefenderCommander, ctx.AttackerCommander, isAttackerSide: false, b);
        ApplyPersonalityCommit(ctx.AttackerCommander, ctx.DefenderCommander, isSelfAttacker: true, b);
        ApplyPersonalityCommit(ctx.DefenderCommander, ctx.AttackerCommander, isSelfAttacker: false, b);
        TryRecklessCommit(ctx.AttackerCommander, ctx.GameData, b);
        TryRecklessCommit(ctx.DefenderCommander, ctx.GameData, b);
    }

    /// <summary>将领属性对战力与胜率的逐项修正。</summary>
    private static void ApplyCommanderStats(
        Character? self,
        Character? enemy,
        bool isAttackerSide,
        BattleFactorBreakdown b)
    {
        if (self is null)
            return;

        // 业务：统率映射 0.88～1.12 战力，武力映射 0.94～1.06 战力
        var leadershipScale = 0.88 + self.Leadership / 100.0 * 0.24;
        var powerScale = 0.94 + self.Power / 100.0 * 0.12;

        if (isAttackerSide)
            b.AttackerPowerScale *= leadershipScale * powerScale;
        else
            b.DefenderPowerScale *= leadershipScale * powerScale;

        if (self.IsSick)
        {
            if (isAttackerSide)
                b.AttackerWinRateDelta -= 8;
            else
                b.DefenderWinRateDelta -= 8;
        }

        if (self.Hp > 0 && self.Hp < 40)
        {
            if (isAttackerSide)
                b.AttackerWinRateDelta -= 5;
            else
                b.DefenderWinRateDelta -= 5;
        }

        // 业务：守方智谋领先敌方每 10 点 +1% 胜率
        if (!isAttackerSide && enemy is not null)
        {
            var strategyEdge = (self.Strategy - enemy.Strategy) / 10;
            if (strategyEdge != 0)
                b.DefenderWinRateDelta += strategyEdge;
        }

        // 业务：军事熟练每级 +1% 胜率，上限 +6%
        var militaryLevel = self.Proficiency.Military.Level;
        if (militaryLevel > 1)
        {
            var bonus = Math.Min(6, militaryLevel - 1);
            if (isAttackerSide)
                b.AttackerWinRateDelta += bonus;
            else
                b.DefenderWinRateDelta += bonus;
        }
    }

    /// <summary>性格对强袭意愿的胜率修正（勇/野心增、慎重减）。</summary>
    private static void ApplyPersonalityCommit(
        Character? self,
        Character? enemy,
        bool isSelfAttacker,
        BattleFactorBreakdown b)
    {
        if (self is null)
            return;

        // 业务：勇气≥75 +5%，野心≥70 +4%，慎重≥78 -6%，敌慎重≥78 +3%
        var delta = 0;
        if (self.Personality.Courage >= 75) delta += 5;
        if (self.Personality.Ambition >= 70) delta += 4;
        if (self.Personality.Action >= 78) delta -= 6;
        if (enemy is not null && enemy.Personality.Action >= 78) delta += 3;

        if (delta == 0)
            return;

        if (isSelfAttacker)
            b.AttackerWinRateDelta += delta;
        else
            b.DefenderWinRateDelta += delta;
    }

    /// <summary>低概率触发轻率莽撞强袭（无视胜率门禁）。</summary>
    private static void TryRecklessCommit(Character? commander, GameData gameData, BattleFactorBreakdown b)
    {
        if (commander is null)
            return;

        // 业务：暴躁勇将或野心莽夫累积轻率分，达 3 分且日期种子命中时 ForceCommit
        var recklessScore = 0;
        if (commander.Personality.Temper <= 25 && commander.Personality.Courage >= 70)
            recklessScore += 2;
        if (commander.Personality.Ambition >= 85 && commander.Personality.Action <= 30)
            recklessScore += 2;

        if (recklessScore < 3)
            return;

        var rollSeed = HashCode.Combine(
            gameData.GameDate.Year,
            gameData.GameDate.Month,
            gameData.GameDate.Day,
            commander.Id,
            recklessScore);
        if (Math.Abs(rollSeed) % 11 == 0)
        {
            b.ForceCommit = true;
            b.Add("commander.reckless", "将领轻率莽撞", 0, detail: commander.Name);
        }
    }

    /// <summary>单位姿态、状态与方针对战力/胜率/伤亡的修正。</summary>
    private static void ApplyPostureFactors(BattleEvaluationContext ctx, BattleFactorBreakdown b)
    {
        ApplyStance(ctx.Attacker, isAttacker: true, b);
        ApplyStance(ctx.Defender, isAttacker: false, b);
        ApplyStatus(ctx.Attacker, isAttacker: true, b);
        ApplyStatus(ctx.Defender, isAttacker: false, b);
        ApplyDirective(ctx.Attacker, isAttacker: true, b);
        ApplyDirective(ctx.Defender, isAttacker: false, b);
    }

    /// <summary>姿态修正：攻方进攻 +5% 战力，守方坚守 +12% 战力/+4% 胜率等。</summary>
    private static void ApplyStance(Unit unit, bool isAttacker, BattleFactorBreakdown b)
        => Policies.Battle.UnitStanceBattleEffectRegistry.Apply(unit, isAttacker, b);

    private static void ApplyStatus(Unit unit, bool isAttacker, BattleFactorBreakdown b)
        => Policies.Battle.UnitStatusBattleEffectRegistry.Apply(unit, isAttacker, b);

    private static void ApplyDirective(Unit unit, bool isAttacker, BattleFactorBreakdown b)
        => Policies.Battle.UnitDirectiveBattleEffectRegistry.Apply(unit, isAttacker, b);

    /// <summary>敌方补给断绝/紧张与携粮天数对己方胜率的加成。</summary>
    private static void ApplySupplyFactors(BattleEvaluationContext ctx, BattleFactorBreakdown b)
    {
        ApplyEnemySupplyForCommit(ctx.Attacker, ctx.Defender, ctx.GameData, isAttackerPerspective: true, b);
        ApplyEnemySupplyForCommit(ctx.Defender, ctx.Attacker, ctx.GameData, isAttackerPerspective: false, b);
    }

    /// <summary>评估敌方补给状态与携粮天数，转化为己方强袭胜率加成。</summary>
    private static void ApplyEnemySupplyForCommit(
        Unit self,
        Unit enemy,
        GameData gameData,
        bool isAttackerPerspective,
        BattleFactorBreakdown b)
    {
        // 业务：敌补给断绝 +15% 胜率，紧张 +8%
        var mod = SupplyStatusEvaluator.EvaluateStatus(enemy, gameData) switch
        {
            SupplyStatusEvaluator.CutOff => 15,
            SupplyStatusEvaluator.Strained => 8,
            _ => 0
        };

        if (mod == 0)
            return;

        if (isAttackerPerspective)
            b.AttackerWinRateDelta += mod;
        else
            b.DefenderWinRateDelta += mod;

        var foodDays = SupplyStatusEvaluator.EstimateFoodDaysRemaining(enemy);
        // 业务：敌携粮 ≤3 日 +12%，≤7 日 +6%
        var foodMod = foodDays switch
        {
            <= 3 => 12,
            <= 7 => 6,
            _ => 0
        };

        if (foodMod == 0)
            return;

        if (isAttackerPerspective)
            b.AttackerWinRateDelta += foodMod;
        else
            b.DefenderWinRateDelta += foodMod;
    }

    /// <summary>远程兵种占比与天气对远程战力的衰减。</summary>
    private static void ApplyCompositionFactors(BattleEvaluationContext ctx, BattleFactorBreakdown b)
    {
        if (ctx.Attacker.SubUnitIds.Count == 0 && ctx.Defender.SubUnitIds.Count == 0)
            return;

        var terrain = ResolveDefenderTerrain(ctx);
        var atkHasRanged = HasRangedDominance(ctx.Attacker, ctx.GameData);
        var defHasRanged = HasRangedDominance(ctx.Defender, ctx.GameData);

        if (atkHasRanged || defHasRanged)
        {
            var weather = BattleWeatherEvaluator.Evaluate(
                ctx.GameData.GameDate,
                ctx.MapMaster,
                ctx.Defender.Location);
            if (weather.ArcherMatchlockScale < 1.0)
            {
                if (atkHasRanged)
                    b.AttackerPowerScale *= weather.ArcherMatchlockScale;
                if (defHasRanged)
                    b.DefenderPowerScale *= weather.ArcherMatchlockScale;
            }
        }

        _ = terrain;
    }

    /// <summary>远程兵力占比 ≥40% 视为远程主导编制。</summary>
    private static bool HasRangedDominance(Unit unit, GameData gameData)
    {
        var ranged = 0;
        var total = 0;
        foreach (var subUnitId in unit.SubUnitIds)
        {
            if (!gameData.SubUnits.TryGetValue(subUnitId, out var sub))
                continue;

            total += sub.Soldier;
            if (sub.TypeId is StrategyTroopTypes.Archer or StrategyTroopTypes.Matchlock)
                ranged += sub.Soldier;
        }

        return total > 0 && ranged * 100 / total >= 40;
    }

    private static TerrainType? ResolveDefenderTerrain(BattleEvaluationContext ctx)
    {
        if (ctx.MapMaster is null)
            return null;

        return ResolveTerrain(ctx.MapMaster, ctx.Defender.Location)?.Type;
    }

    /// <summary>日期与区域气候的胜率修正，写入因素明细。</summary>
    private static void ApplyWeatherFactors(BattleEvaluationContext ctx, BattleFactorBreakdown b)
    {
        var weather = BattleWeatherEvaluator.Evaluate(
            ctx.GameData.GameDate,
            ctx.MapMaster,
            ctx.Defender.Location);

        if (weather.AttackerWinRateDelta != 0 || weather.DefenderWinRateDelta != 0)
        {
            b.AttackerWinRateDelta += weather.AttackerWinRateDelta;
            b.DefenderWinRateDelta += weather.DefenderWinRateDelta;
            b.Add("weather", $"天气({weather.Label})", weather.AttackerWinRateDelta, weather.DefenderWinRateDelta);
        }
    }

    /// <summary>计略迷惑与粮道情报误导；守方中计可触发莽撞强袭。</summary>
    private static void ApplyStratagemFactors(BattleEvaluationContext ctx, BattleFactorBreakdown b)
    {
        var snapshot = BattleStratagemEvaluator.Evaluate(ctx);
        BattleStratagemEvaluator.ApplyToBreakdown(snapshot, b);

        if (snapshot.DefenderDeceived || snapshot.DefenderSupplyDeceived)
            b.ForceCommit = true;
    }

    /// <summary>守方所在地形对守方胜率的加成（山地 +6%、丘陵 +4% 等）。</summary>
    private static void ApplyTerrainFactors(BattleEvaluationContext ctx, BattleFactorBreakdown b)
    {
        if (ctx.MapMaster is null)
            return;

        var defenderTerrain = ResolveTerrain(ctx.MapMaster, ctx.Defender.Location);
        if (defenderTerrain is null)
            return;

        var terrainMod = defenderTerrain.Type switch
        {
            TerrainType.Mountain => 6,
            TerrainType.Hill => 4,
            TerrainType.Badlands => 2,
            _ => 0
        };

        if (terrainMod > 0)
            b.DefenderWinRateDelta += terrainMod;
    }

    private static TerrainDefinition? ResolveTerrain(GameMapMasterData mapMaster, Point3 location)
    {
        var terrainId = mapMaster.TileMap.GetTerrain(location);
        return mapMaster.Terrains.GetValueOrDefault(terrainId);
    }

    /// <summary>对峙日久双方均获得情报加成（每 2 日 +1%，上限 +10%）。</summary>
    private static void ApplyStandoffIntelFactors(BattleEvaluationContext ctx, BattleFactorBreakdown b)
    {
        if (ctx.StandoffDays <= 0)
            return;

        // 业务：对峙情报加成双方平分
        var intelBonus = Math.Min(10, ctx.StandoffDays / 2);
        b.AttackerWinRateDelta += intelBonus / 2;
        b.DefenderWinRateDelta += intelBonus / 2;
    }

    /// <summary>2 格内友军增援与敌军威慑对胜率的修正。</summary>
    private static void ApplyReinforcementFactors(BattleEvaluationContext ctx, BattleFactorBreakdown b)
    {
        var atkReinforce = CountNearbyAllies(ctx.Attacker, ctx.GameData, radius: 2);
        var defReinforce = CountNearbyAllies(ctx.Defender, ctx.GameData, radius: 2);
        var atkEnemyPressure = CountNearbyHostiles(ctx.Attacker, ctx.GameData, radius: 2);
        var defEnemyPressure = CountNearbyHostiles(ctx.Defender, ctx.GameData, radius: 2);

        // 业务：每支友军 +3% 胜率，上限 +8%
        if (atkReinforce > 0)
        {
            var delta = Math.Min(8, atkReinforce * 3);
            b.AttackerWinRateDelta += delta;
            b.Add("reinforce.allies", $"友军增援×{atkReinforce}", delta, 0, "附近友军");
        }

        if (defReinforce > 0)
        {
            var delta = Math.Min(8, defReinforce * 3);
            b.DefenderWinRateDelta += delta;
            b.Add("reinforce.allies_def", $"守方友军×{defReinforce}", 0, delta, "附近友军");
        }

        // 业务：侧翼敌军威慑每支 -3% 胜率，上限 -10%
        if (atkEnemyPressure > 0)
        {
            var penalty = Math.Min(10, atkEnemyPressure * 3);
            b.AttackerWinRateDelta -= penalty;
            b.Add("threat.nearby_enemies", $"侧翼敌军×{atkEnemyPressure}", -penalty, 0, "防偷袭");
        }

        if (defEnemyPressure > 0)
        {
            var penalty = Math.Min(10, defEnemyPressure * 3);
            b.DefenderWinRateDelta -= penalty;
            b.Add("threat.nearby_enemies_def", $"守方侧翼敌军×{defEnemyPressure}", 0, -penalty, "防偷袭");
        }
    }

    private static int CountNearbyAllies(Unit unit, GameData gameData, int radius)
    {
        var count = 0;
        foreach (var other in gameData.Units.Values)
        {
            if (other.Id == unit.Id || other.ForceId != unit.ForceId || other.Soldier <= 0)
                continue;

            if (Manhattan(unit.Location, other.Location) <= radius)
                count++;
        }

        return count;
    }

    private static int CountNearbyHostiles(Unit unit, GameData gameData, int radius)
    {
        if (!gameData.Forces.TryGetValue(unit.ForceId, out var myForce))
            return 0;

        var count = 0;
        foreach (var other in gameData.Units.Values)
        {
            if (other.Id == unit.Id || other.ForceId == unit.ForceId || other.Soldier <= 0)
                continue;

            if (Manhattan(unit.Location, other.Location) > radius)
                continue;

            if (!gameData.Forces.TryGetValue(other.ForceId, out var otherForce))
                continue;

            if (Domain.Rules.DiplomacyRules.IsEnemy(myForce, otherForce).IsSuccess)
                count++;
        }

        return count;
    }

    private static int Manhattan(Point3 a, Point3 b)
        => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    /// <summary>Commit 阶段门禁：任一方不具备接敌资格则禁止强袭。</summary>
    private static void ApplyPhaseGates(BattleEvaluationContext ctx, BattleFactorBreakdown b)
    {
        if (ctx.Phase != BattleEvaluationPhase.Commit)
            return;

        if (!CanUnitEngage(ctx.Attacker) || !CanUnitEngage(ctx.Defender))
            b.BlockCommit = true;
    }
}
