using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Battle;

/// <summary>战术自动战斗结果（含战报过程，不含因素修正描述）。</summary>
public sealed class TacticalBattleResult
{
    public required InstantBattleOutcome Outcome { get; init; }
    public required IReadOnlyList<StrategyBattleLogEntryDto> LogEntries { get; init; }
    public required IReadOnlyDictionary<int, int> CasualtiesByUnitId { get; init; }
    /// <summary>真实 SubUnit Id → 战后兵数；无编制单位仅有 CasualtiesByUnitId。</summary>
    public required IReadOnlyDictionary<int, int> SubUnitSoldiersAfter { get; init; }
    /// <summary>守方主队是否陷入四邻围攻。</summary>
    public bool IsSurrounded { get; init; }
    /// <summary>攻方参战单位数（含附近驰援）。</summary>
    public int AttackerParticipantCount { get; init; }
    /// <summary>守方参战单位数（含附近驰援）。</summary>
    public int DefenderParticipantCount { get; init; }
    /// <summary>攻方参战单位 Id（含主队）。</summary>
    public IReadOnlyList<int> AttackerParticipantUnitIds { get; init; } = [];
    /// <summary>守方参战单位 Id（含主队）。</summary>
    public IReadOnlyList<int> DefenderParticipantUnitIds { get; init; } = [];
    /// <summary>攻方是否因移动力取得互令先手。</summary>
    public bool MovementInitiative { get; init; }
}

/// <summary>
/// 自动战斗战术模拟：展开子单位行动、将领判定、地图围攻布局，并生成过程战报。
/// </summary>
public static class TacticalBattleSimulator
{
    /// <summary>战术模拟最大回合数（当日决战分多轮交锋）。</summary>
    private const int MaxRounds = 4;

    private sealed class Combatant
    {
        public required Unit Parent { get; init; }
        public required SubUnit SubUnit { get; init; }
        public required bool IsAttacker { get; init; }
        public int Soldiers { get; set; }
        public int InitialSoldiers { get; init; }
        public string DisplayName { get; init; } = "";
        public BattleFormationSlot Slot { get; set; }
        public int Movement => SubUnit.Movement > 0 ? SubUnit.Movement : Parent.Movement;
        public int Attack => SubUnit.Attack > 0
            ? SubUnit.Attack
            : Parent.Attack > 0 ? Parent.Attack : BattleConstants.DefaultCombatStat;
        public int Defense => SubUnit.Defense > 0
            ? SubUnit.Defense
            : Parent.Defense > 0 ? Parent.Defense : BattleConstants.DefaultCombatStat;
        public byte TypeId => SubUnit.TypeId;
    }

    /// <summary>执行战术决战模拟：布阵、多回合交锋、伤亡汇总与战报生成。</summary>
    public static TacticalBattleResult Resolve(
        Unit primaryAttacker,
        Unit primaryDefender,
        GameData gameData,
        int seed,
        GameMapMasterData? mapMaster = null,
        bool bothOrderedAttack = false,
        string? commitReason = null,
        BattleFactorBreakdown? factorBreakdown = null)
    {
        var battlefield = BattleBattlefieldAssembler.Assemble(primaryAttacker, primaryDefender, gameData);
        var rng = new Random(seed);
        var logs = new List<StrategyBattleLogEntryDto>();
        var order = 0;
        var atkCasualtyScale = factorBreakdown?.AttackerCasualtyScale ?? 1.0;
        var defCasualtyScale = factorBreakdown?.DefenderCasualtyScale ?? 1.0;

        void Add(string side, string phase, string message) =>
            logs.Add(new StrategyBattleLogEntryDto
            {
                Order = ++order,
                Side = side,
                Phase = phase,
                Message = message
            });

        // 业务：移动力高者互下攻击令时取得先手并担任攻方
        var movementInitiative = primaryAttacker.Movement >= primaryDefender.Movement;
        Add("system", "接触", $"{primaryAttacker.Name} 与 {primaryDefender.Name} 接敌，展开野战。");
        if (!string.IsNullOrWhiteSpace(commitReason))
            Add("system", "强袭", commitReason!);

        if (bothOrderedAttack)
        {
            Add(
                "system",
                "先手",
                movementInitiative
                    ? $"{primaryAttacker.Name} 与 {primaryDefender.Name} 互下攻击令；{primaryAttacker.Name} 移动力较高（{primaryAttacker.Movement}）先进攻，担任攻方。"
                    : $"{primaryAttacker.Name} 与 {primaryDefender.Name} 互下攻击令；{primaryAttacker.Name} 先行动并担任攻方。");
        }
        else
        {
            Add(
                "system",
                "先手",
                $"{primaryAttacker.Name} 移动力 {primaryAttacker.Movement}，率先发起进攻并担任攻方。");
        }

        Add(
            "system",
            "布阵",
            $"以 {primaryDefender.Name} 为中心，周围 {BattleBattlefieldAssembler.ParticipationRadius} 格内兵队入场：" +
            $"攻方 {battlefield.AttackerUnits.Count} 队（{battlefield.AttackerSoldiers} 人），" +
            $"守方 {battlefield.DefenderUnits.Count} 队（{battlefield.DefenderSoldiers} 人）。");

        if (battlefield.IsSurrounded)
        {
            Add(
                "system",
                "围攻",
                $"{primaryDefender.Name} 上下左右四邻皆为 {primaryAttacker.Name} 方兵队，陷入围攻。");
        }

        foreach (var u in battlefield.AttackerUnits.Where(u => u.Id != primaryAttacker.Id))
            Add("attacker", "参战", $"{u.Name}（{u.Soldier} 人）自附近赶来参战。");
        foreach (var u in battlefield.DefenderUnits.Where(u => u.Id != primaryDefender.Id))
            Add("defender", "参战", $"{u.Name}（{u.Soldier} 人）驰援守军。");

        var attackerCombatants = ExpandCombatants(battlefield.AttackerUnits, isAttacker: true, gameData);
        var defenderCombatants = ExpandCombatants(battlefield.DefenderUnits, isAttacker: false, gameData);

        AssignFormationSlots(attackerCombatants, isAttacker: true);
        AssignFormationSlots(defenderCombatants, isAttacker: false);

        Add(
            "system",
            "展开",
            $"双方展开子队并布阵：攻方 {attackerCombatants.Count} 支、守方 {defenderCombatants.Count} 支。");
        LogFormation(attackerCombatants, "attacker", Add);
        LogFormation(defenderCombatants, "defender", Add);

        var atkPower = Math.Max(1, SumPower(attackerCombatants, mapMaster, primaryDefender));
        var defPower = Math.Max(1, SumPower(defenderCombatants, mapMaster, primaryDefender));
        var winRate = (int)Math.Clamp(
            Math.Round((double)atkPower / (atkPower + defPower) * 100),
            BattleConstants.MinWinRatePercent,
            BattleConstants.MaxWinRatePercent);
        // 业务：围攻态势攻方胜率额外 +8%
        if (battlefield.IsSurrounded)
            winRate = Math.Min(BattleConstants.MaxWinRatePercent, winRate + 8);

        var atkCommander = ResolveCommander(primaryAttacker, gameData);
        var defCommander = ResolveCommander(primaryDefender, gameData);
        var atkInitial = attackerCombatants.Sum(c => c.InitialSoldiers);
        var defInitial = defenderCombatants.Sum(c => c.InitialSoldiers);

        BattleCommanderActionRules.CommanderDecision? atkDecision = null;
        BattleCommanderActionRules.CommanderDecision? defDecision = null;

        var primaryAtkBefore = primaryAttacker.Soldier;
        var primaryDefBefore = primaryDefender.Soldier;

        for (var round = 1; round <= MaxRounds; round++)
        {
            if (!attackerCombatants.Exists(c => c.Soldiers > 0) || !defenderCombatants.Exists(c => c.Soldiers > 0))
                break;

            var atkRemain = attackerCombatants.Sum(c => c.Soldiers);
            var defRemain = defenderCombatants.Sum(c => c.Soldiers);
            var atkRatio = atkInitial > 0 ? (double)atkRemain / atkInitial : 0;
            var defRatio = defInitial > 0 ? (double)defRemain / defInitial : 0;
            // 业务：按当前余兵比动态估算回合胜率
            var roundWinRate = (int)Math.Clamp(
                Math.Round(atkRatio / Math.Max(0.01, atkRatio + defRatio) * 100),
                BattleConstants.MinWinRatePercent,
                BattleConstants.MaxWinRatePercent);

            var prevAtk = atkDecision?.Action;
            var prevDef = defDecision?.Action;
            atkDecision = BattleCommanderActionRules.Decide(
                atkCommander, primaryAttacker, defCommander, isAttacker: true,
                battlefield.IsSurrounded, roundWinRate, rng, atkRatio, defRatio, prevAtk);
            defDecision = BattleCommanderActionRules.Decide(
                defCommander, primaryDefender, atkCommander, isAttacker: false,
                battlefield.IsSurrounded, 100 - roundWinRate, rng, defRatio, atkRatio, prevDef);

            Add("system", "回合", $"── 第 {round} 回合 ──");
            if (round == 1 || prevAtk != atkDecision.Value.Action)
                Add("attacker", "将令", atkDecision.Value.Description);
            else
                Add("attacker", "将令", $"攻方主将继续{BattleCommanderActionRules.ActionVerb(atkDecision.Value.Action)}。");

            if (round == 1 || prevDef != defDecision.Value.Action)
                Add("defender", "将令", defDecision.Value.Description);
            else
                Add("defender", "将令", $"守方主将继续{BattleCommanderActionRules.ActionVerb(defDecision.Value.Action)}。");

            // 业务：双方均意图脱离则提前结束当日交锋
            if (atkDecision.Value.Action == BattleCommanderActionKind.Withdraw
                && defDecision.Value.Action == BattleCommanderActionKind.Withdraw)
            {
                Add("system", "脱离", "双方均意图脱离，交锋草草收场。");
                break;
            }

            // 业务：按移动力降序行动，同速攻方优先
            var actors = attackerCombatants.Concat(defenderCombatants)
                .Where(c => c.Soldiers > 0)
                .OrderByDescending(c => c.Movement)
                .ThenBy(c => c.IsAttacker ? 0 : 1)
                .ThenBy(c => c.Parent.Id)
                .ThenBy(c => c.SubUnit.Id)
                .ToList();

            foreach (var actor in actors)
            {
                if (actor.Soldiers <= 0)
                    continue;

                var decision = actor.IsAttacker ? atkDecision.Value : defDecision.Value;
                var enemyDecision = actor.IsAttacker ? defDecision.Value : atkDecision.Value;
                // 业务：脱离意图下 40% 概率该子队本回合不突击
                if (decision.Action == BattleCommanderActionKind.Withdraw && rng.Next(100) < 40)
                {
                    Add(
                        actor.IsAttacker ? "attacker" : "defender",
                        "脱离",
                        $"{actor.DisplayName} 奉命收缩，未主动突击。");
                    continue;
                }

                var enemies = actor.IsAttacker ? defenderCombatants : attackerCombatants;
                var livingEnemies = enemies.Where(e => e.Soldiers > 0).ToList();
                if (livingEnemies.Count == 0)
                    break;

                var commanderParentId = actor.IsAttacker ? primaryDefender.Id : primaryAttacker.Id;
                var target = BattleTargetScoring.PickBestTarget(
                    livingEnemies,
                    e => e.Soldiers,
                    e => e.Defense,
                    e => e.TypeId,
                    e => e.Slot,
                    e => e.Parent.Id == commanderParentId,
                    actor.Slot,
                    actor.TypeId,
                    decision.Action,
                    rng);

                var damage = ComputeStrikeDamage(
                    actor,
                    target,
                    decision,
                    enemyDecision,
                    battlefield.IsSurrounded,
                    actor.IsAttacker,
                    mapMaster,
                    primaryDefender,
                    rng,
                    actor.IsAttacker ? atkCasualtyScale : defCasualtyScale,
                    actor.IsAttacker ? defCasualtyScale : atkCasualtyScale);

                if (damage <= 0)
                {
                    Add(
                        actor.IsAttacker ? "attacker" : "defender",
                        "交锋",
                        $"{actor.DisplayName}[{BattleFormationSlotRules.SlotLabel(actor.Slot)}] 突击 {target.DisplayName}，未能造成有效杀伤。");
                    continue;
                }

                target.Soldiers = Math.Max(0, target.Soldiers - damage);
                Add(
                    actor.IsAttacker ? "attacker" : "defender",
                    "交锋",
                    $"{actor.DisplayName}[{BattleFormationSlotRules.SlotLabel(actor.Slot)}] 冲击 {target.DisplayName}[{BattleFormationSlotRules.SlotLabel(target.Slot)}]，斩获 {damage} 人（敌余 {target.Soldiers}）。");
            }

            winRate = roundWinRate;
        }

        var atkRemaining = attackerCombatants.Sum(c => c.Soldiers);
        var defRemaining = defenderCombatants.Sum(c => c.Soldiers);
        // 业务：余兵多者胜；同余兵时按胜率 ≥50% 判攻方胜
        var attackerWon = atkRemaining > defRemaining
            || (atkRemaining == defRemaining && winRate >= 50);

        // 仅汇总伤亡与子队余兵，不写回世界（由结算系统 Apply）
        var casualtiesByUnit = new Dictionary<int, int>();
        var subUnitAfter = new Dictionary<int, int>();
        CollectCasualties(attackerCombatants, casualtiesByUnit, subUnitAfter);
        CollectCasualties(defenderCombatants, casualtiesByUnit, subUnitAfter);

        var primaryAtkCasualties = casualtiesByUnit.GetValueOrDefault(primaryAttacker.Id);
        var primaryDefCasualties = casualtiesByUnit.GetValueOrDefault(primaryDefender.Id);

        // 业务：双方开战前均有兵时，主队至少各计 1 伤亡，避免零伤亡僵局
        if (primaryAtkBefore > 0 && primaryDefBefore > 0)
        {
            if (primaryAtkCasualties <= 0)
            {
                primaryAtkCasualties = 1;
                casualtiesByUnit[primaryAttacker.Id] = 1;
                ForceMinimumSubUnitLoss(primaryAttacker, subUnitAfter, gameData);
            }

            if (primaryDefCasualties <= 0)
            {
                primaryDefCasualties = 1;
                casualtiesByUnit[primaryDefender.Id] = 1;
                ForceMinimumSubUnitLoss(primaryDefender, subUnitAfter, gameData);
            }
        }

        var roll = attackerWon
            ? rng.Next(0, Math.Max(1, winRate))
            : rng.Next(winRate, 100);

        if (attackerWon)
        {
            Add("attacker", "突破", "攻方攻势得手，守军阵线崩溃。");
            Add("defender", "败势", $"{primaryDefender.Name} 一方败象已成，当日无力再战。");
            Add("system", "结束", "攻方获胜，当日野战结束。败方转入重整（不强制后撤一格）。");
        }
        else
        {
            Add("defender", "反击", "守军顶住攻势，反将攻方击退。");
            Add("attacker", "受挫", $"{primaryAttacker.Name} 一方攻势受挫，收兵重整。");
            Add("system", "结束", "守方获胜，当日野战结束。败方转入重整（不强制后撤一格）。");
        }

        var outcome = new InstantBattleOutcome(
            AttackerWon: attackerWon,
            AttackerWinRatePercent: winRate,
            AttackerCasualties: primaryAtkCasualties,
            DefenderCasualties: primaryDefCasualties,
            ResolutionSeed: seed,
            ResolutionRoll: roll,
            AttackerSoldiersBefore: primaryAtkBefore,
            DefenderSoldiersBefore: primaryDefBefore);

        return new TacticalBattleResult
        {
            Outcome = outcome,
            LogEntries = logs,
            CasualtiesByUnitId = casualtiesByUnit,
            SubUnitSoldiersAfter = subUnitAfter,
            IsSurrounded = battlefield.IsSurrounded,
            AttackerParticipantCount = battlefield.AttackerUnits.Count,
            DefenderParticipantCount = battlefield.DefenderUnits.Count,
            AttackerParticipantUnitIds = battlefield.AttackerUnits.Select(u => u.Id).ToList(),
            DefenderParticipantUnitIds = battlefield.DefenderUnits.Select(u => u.Id).ToList(),
            MovementInitiative = movementInitiative
        };
    }

    /// <summary>将参战单位展开为子队战斗体；无编制时合成虚拟本队。</summary>
    private static List<Combatant> ExpandCombatants(IReadOnlyList<Unit> units, bool isAttacker, GameData gameData)
    {
        var list = new List<Combatant>();
        foreach (var unit in units)
        {
            var any = false;
            foreach (var subId in unit.SubUnitIds)
            {
                if (!gameData.SubUnits.TryGetValue(subId, out var sub) || sub.Soldier <= 0)
                    continue;

                any = true;
                var typeName = StrategyTroopTypes.ResolveName(sub.TypeId, sub.TypeName);
                list.Add(new Combatant
                {
                    Parent = unit,
                    SubUnit = sub,
                    IsAttacker = isAttacker,
                    Soldiers = sub.Soldier,
                    InitialSoldiers = sub.Soldier,
                    DisplayName = $"{unit.Name}·{typeName}"
                });
            }

            if (!any && unit.Soldier > 0)
            {
                // 业务：无 SubUnit 编制时以足轻本队虚拟子队参战
                var synthetic = new SubUnit
                {
                    Id = -unit.Id,
                    UnitId = unit.Id,
                    ForceId = unit.ForceId,
                    Soldier = unit.Soldier,
                    Attack = unit.Attack,
                    Defense = unit.Defense,
                    Movement = unit.Movement,
                    TypeId = StrategyTroopTypes.Ashigaru,
                    TypeName = "本队"
                };
                list.Add(new Combatant
                {
                    Parent = unit,
                    SubUnit = synthetic,
                    IsAttacker = isAttacker,
                    Soldiers = unit.Soldier,
                    InitialSoldiers = unit.Soldier,
                    DisplayName = $"{unit.Name}·本队"
                });
            }
        }

        return list;
    }

    /// <summary>汇总所有子队有效战力（含地形兵种系数）。</summary>
    private static int SumPower(IEnumerable<Combatant> combatants, GameMapMasterData? mapMaster, Unit defender)
    {
        TerrainType? terrain = null;
        if (mapMaster is not null)
        {
            var terrainId = mapMaster.TileMap.GetTerrain(defender.Location);
            if (mapMaster.Terrains.TryGetValue(terrainId, out var def))
                terrain = def.Type;
        }

        var total = 0;
        foreach (var c in combatants)
        {
            var scale = BattleCompositionCalculator.ResolveTroopTypeScale(c.SubUnit.TypeId, terrain);
            total += (int)Math.Round(c.Soldiers * (c.Attack + c.Defense) / 20.0 * scale);
        }

        return total;
    }

    /// <summary>计算单次突击伤害：兵种、阵位、将令、围攻、伤亡系数与随机波动。</summary>
    private static int ComputeStrikeDamage(
        Combatant actor,
        Combatant target,
        BattleCommanderActionRules.CommanderDecision ownDecision,
        BattleCommanderActionRules.CommanderDecision enemyDecision,
        bool isSurrounded,
        bool actorIsAttacker,
        GameMapMasterData? mapMaster,
        Unit defender,
        Random rng,
        double _ = 1.0,
        double targetCasualtyScale = 1.0)
    {
        TerrainType? terrain = null;
        if (mapMaster is not null)
        {
            var terrainId = mapMaster.TileMap.GetTerrain(defender.Location);
            if (mapMaster.Terrains.TryGetValue(terrainId, out var def))
                terrain = def.Type;
        }

        // 业务：基础伤害 = 兵数×攻/(防+4) × 兵种系数 × 0.35 × 将令倍率 × 目标 CasualtyScale
        var typeScale = BattleCompositionCalculator.ResolveTroopTypeScale(actor.SubUnit.TypeId, terrain);
        var raw = actor.Soldiers * actor.Attack / Math.Max(8, target.Defense + 4);
        raw = (int)Math.Round(raw * typeScale * 0.35);
        raw = (int)Math.Round(raw * ownDecision.OwnDamageScale * enemyDecision.OwnTakenScale);
        // 业务：因素分解中的目标方伤亡系数（撤退减伤等）
        raw = (int)Math.Round(raw * targetCasualtyScale);

        // 业务：近战按阵位距离衰减（0格100%、1格92%、2格75%、更远55%）
        var dist = BattleFormationSlotRules.SlotDistance(actor.Slot, target.Slot);
        if (!BattleFormationSlotRules.IsRanged(actor.TypeId))
        {
            raw = dist switch
            {
                0 => raw,
                1 => (int)Math.Round(raw * 0.92),
                2 => (int)Math.Round(raw * 0.75),
                _ => (int)Math.Round(raw * 0.55)
            };
        }

        // 业务：骑兵冲击远程 +20%；围攻攻方 +25%、守方 -15%
        if (BattleFormationSlotRules.IsCavalry(actor.TypeId)
            && BattleFormationSlotRules.IsRanged(target.TypeId))
            raw = (int)Math.Round(raw * 1.20);

        if (isSurrounded)
        {
            if (actorIsAttacker)
                raw = (int)Math.Round(raw * 1.25);
            else
                raw = (int)Math.Round(raw * 0.85);
        }

        // 业务：侧击意图打击翼/后军 +15%
        if (ownDecision.Action == BattleCommanderActionKind.Flank
            && (BattleFormationSlotRules.IsFlank(target.Slot) || target.Slot == BattleFormationSlot.Rear))
            raw = (int)Math.Round(raw * 1.15);

        // 业务：±2 随机波动，伤害至少 1、不超过目标余兵
        raw = Math.Max(1, raw + rng.Next(0, 3));
        return Math.Min(target.Soldiers, raw);
    }

    /// <summary>按兵种与移动力为子队分配战术阵位。</summary>
    private static void AssignFormationSlots(List<Combatant> combatants, bool isAttacker)
    {
        BattleFormationSlotRules.AssignSlots(
            combatants,
            c => c.TypeId,
            c => c.Movement,
            c => c.SubUnit.Id,
            (c, slot) => c.Slot = slot,
            isAttacker);
    }

    private static void LogFormation(
        List<Combatant> combatants,
        string side,
        Action<string, string, string> add)
    {
        foreach (var group in combatants.GroupBy(c => c.Slot).OrderBy(g => g.Key))
        {
            var names = string.Join("、", group.Select(c => c.DisplayName));
            add(side, "布阵", $"{BattleFormationSlotRules.SlotLabel(group.Key)}：{names}");
        }
    }

    private static void ForceMinimumSubUnitLoss(Unit unit, Dictionary<int, int> subUnitAfter, GameData gameData)
    {
        foreach (var subId in unit.SubUnitIds)
        {
            if (!gameData.SubUnits.TryGetValue(subId, out var sub) || sub.Soldier <= 0)
                continue;

            var current = subUnitAfter.GetValueOrDefault(subId, sub.Soldier);
            if (current > 0)
            {
                subUnitAfter[subId] = current - 1;
                return;
            }
        }
    }

    /// <summary>汇总各单位与子队伤亡，供结算系统写回。</summary>
    private static void CollectCasualties(
        List<Combatant> combatants,
        Dictionary<int, int> casualtiesByUnit,
        Dictionary<int, int> subUnitAfter)
    {
        foreach (var group in combatants.GroupBy(c => c.Parent.Id))
        {
            var loss = group.Sum(c => c.InitialSoldiers - c.Soldiers);
            var unitId = group.Key;
            casualtiesByUnit[unitId] = casualtiesByUnit.GetValueOrDefault(unitId) + loss;
            foreach (var c in group)
            {
                if (c.SubUnit.Id > 0)
                    subUnitAfter[c.SubUnit.Id] = c.Soldiers;
            }
        }
    }

    /// <summary>将战术模拟伤亡写回单位与 SubUnit。</summary>
    public static void ApplyCasualtiesToWorld(TacticalBattleResult result, GameData gameData)
    {
        foreach (var (subId, soldiers) in result.SubUnitSoldiersAfter)
        {
            if (gameData.SubUnits.TryGetValue(subId, out var sub))
                sub.Soldier = Math.Max(0, soldiers);
        }

        foreach (var (unitId, casualties) in result.CasualtiesByUnitId)
        {
            if (!gameData.Units.TryGetValue(unitId, out var unit))
                continue;

            if (unit.SubUnitIds.Count > 0)
            {
                unit.Soldier = unit.SubUnitIds.Sum(id =>
                    gameData.SubUnits.TryGetValue(id, out var s) ? Math.Max(0, s.Soldier) : 0);
            }
            else
            {
                unit.Soldier = Math.Max(0, unit.Soldier - casualties);
            }

            if (unit.Soldier == 0)
                unit.Status = Unit.UnitStatus.Chaos;
        }
    }

    /// <summary>按子单位当前余兵比例，将单位总伤亡按比例分摊到各 SubUnit。</summary>
    public static void DistributeCasualtiesToSubUnits(Unit unit, int casualties, GameData gameData)
    {
        if (casualties <= 0 || unit.SubUnitIds.Count == 0)
            return;

        var subs = unit.SubUnitIds
            .Select(id => gameData.SubUnits.TryGetValue(id, out var s) ? s : null)
            .Where(s => s is not null && s.Soldier > 0)
            .Cast<SubUnit>()
            .ToList();
        if (subs.Count == 0)
            return;

        var total = subs.Sum(s => s.Soldier);
        if (total <= 0)
            return;

        var remaining = Math.Min(casualties, total);
        for (var i = 0; i < subs.Count; i++)
        {
            var share = i == subs.Count - 1
                ? remaining
                : Math.Min(subs[i].Soldier, remaining * subs[i].Soldier / total);
            share = Math.Min(subs[i].Soldier, Math.Max(0, share));
            subs[i].Soldier -= share;
            remaining -= share;
            if (remaining <= 0)
                break;
        }

        unit.Soldier = unit.SubUnitIds.Sum(id =>
            gameData.SubUnits.TryGetValue(id, out var s) ? Math.Max(0, s.Soldier) : 0);
    }

    private static Character? ResolveCommander(Unit unit, GameData gameData)
        => unit.LeaderId > 0 && gameData.Characters.TryGetValue(unit.LeaderId, out var c) ? c : null;
}
