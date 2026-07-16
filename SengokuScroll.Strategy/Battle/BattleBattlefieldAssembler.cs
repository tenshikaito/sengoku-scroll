using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Strategy.Battle;

/// <summary>决战战场：以守方主队为中心，半径内友军/敌军入场，并判定四邻围攻。</summary>
public sealed class BattleBattlefield
{
    public required Unit PrimaryAttacker { get; init; }
    public required Unit PrimaryDefender { get; init; }
    public required IReadOnlyList<Unit> AttackerUnits { get; init; }
    public required IReadOnlyList<Unit> DefenderUnits { get; init; }

    /// <summary>守方主队四邻（上下左右）均被攻方势力单位占据。</summary>
    public bool IsSurrounded { get; init; }

    public int AttackerSoldiers => AttackerUnits.Sum(u => Math.Max(0, u.Soldier));
    public int DefenderSoldiers => DefenderUnits.Sum(u => Math.Max(0, u.Soldier));
}

/// <summary>组装战场参战单位与围攻态势。</summary>
public static class BattleBattlefieldAssembler
{
    /// <summary>守方周围参战半径（曼哈顿距离）；仅邻格驰援，避免远处部队被静默卷入。</summary>
    public const int ParticipationRadius = 1;

    /// <summary>组装决战战场：收集半径内参战单位并判定围攻。</summary>
    public static BattleBattlefield Assemble(Unit primaryAttacker, Unit primaryDefender, GameData gameData)
    {
        var attackers = new List<Unit>();
        var defenders = new List<Unit>();

        foreach (var unit in gameData.Units.Values)
        {
            // 业务：无兵或混乱状态不参与决战
            if (unit.Soldier <= 0 || unit.Status == Unit.UnitStatus.Chaos)
                continue;

            var dist = Manhattan(unit.Location, primaryDefender.Location);
            if (dist > ParticipationRadius)
                continue;

            if (unit.ForceId == primaryAttacker.ForceId)
                attackers.Add(unit);
            else if (unit.ForceId == primaryDefender.ForceId)
                defenders.Add(unit);
        }

        if (!attackers.Exists(u => u.Id == primaryAttacker.Id))
            attackers.Insert(0, primaryAttacker);
        if (!defenders.Exists(u => u.Id == primaryDefender.Id))
            defenders.Insert(0, primaryDefender);

        // 主队置顶，其余按距离再按 Id
        attackers = OrderParticipants(attackers, primaryAttacker, primaryDefender.Location);
        defenders = OrderParticipants(defenders, primaryDefender, primaryDefender.Location);

        return new BattleBattlefield
        {
            PrimaryAttacker = primaryAttacker,
            PrimaryDefender = primaryDefender,
            AttackerUnits = attackers,
            DefenderUnits = defenders,
            IsSurrounded = DetectSurround(primaryDefender, primaryAttacker.ForceId, gameData)
        };
    }

    /// <summary>检测守方主队四邻是否均被攻方势力占据（围攻态势）。</summary>
    public static bool DetectSurround(Unit defender, int attackerForceId, GameData gameData)
    {
        var occupiedByAttacker = new HashSet<(int X, int Y)>();
        foreach (var unit in gameData.Units.Values)
        {
            if (unit.ForceId != attackerForceId || unit.Soldier <= 0)
                continue;
            if (Manhattan(unit.Location, defender.Location) != 1)
                continue;
            occupiedByAttacker.Add((unit.Location.X, unit.Location.Y));
        }

        var x = defender.Location.X;
        var y = defender.Location.Y;
        Point3[] neighbors =
        [
            new(x, y - 1),
            new(x + 1, y),
            new(x, y + 1),
            new(x - 1, y)
        ];

        return neighbors.All(n => occupiedByAttacker.Contains((n.X, n.Y)));
    }

    private static List<Unit> OrderParticipants(List<Unit> units, Unit primary, Point3 center)
        => [.. units
            .OrderBy(u => u.Id == primary.Id ? 0 : 1)
            .ThenBy(u => Manhattan(u.Location, center))
            .ThenBy(u => u.Id)];

    private static int Manhattan(Point3 a, Point3 b)
        => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
}
