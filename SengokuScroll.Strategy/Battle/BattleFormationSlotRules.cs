using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;

namespace SengokuScroll.Strategy.Battle;

/// <summary>相对阵位（抽象战术棋盘，非地图格）。</summary>
public enum BattleFormationSlot
{
    /// <summary>前军——近战主力接敌线。</summary>
    Front = 0,
    /// <summary>中军——二线支援与低机动足轻。</summary>
    Mid = 1,
    /// <summary>后军——远程与火枪队默认位。</summary>
    Rear = 2,
    /// <summary>左翼——攻方骑兵默认侧翼。</summary>
    FlankLeft = 3,
    /// <summary>右翼——守方骑兵默认侧翼。</summary>
    FlankRight = 4
}

/// <summary>按兵种与移动力分配相对阵位。</summary>
public static class BattleFormationSlotRules
{
    /// <summary>按兵种与攻守角色返回偏好阵位（远程后军、骑兵侧翼、其余前军）。</summary>
    public static BattleFormationSlot PreferredSlot(byte typeId, bool isAttacker)
        => typeId switch
        {
            StrategyTroopTypes.Archer or StrategyTroopTypes.Matchlock => BattleFormationSlot.Rear,
            StrategyTroopTypes.Cavalry => isAttacker ? BattleFormationSlot.FlankLeft : BattleFormationSlot.FlankRight,
            _ => BattleFormationSlot.Front
        };

    /// <summary>阵位中文标签，用于战报叙述。</summary>
    public static string SlotLabel(BattleFormationSlot slot) => slot switch
    {
        BattleFormationSlot.Front => "前军",
        BattleFormationSlot.Mid => "中军",
        BattleFormationSlot.Rear => "后军",
        BattleFormationSlot.FlankLeft => "左翼",
        BattleFormationSlot.FlankRight => "右翼",
        _ => "阵列"
    };

    /// <summary>
    /// 高移动力优先占偏好位；同偏好按移动力再按 Id。
    /// 骑兵左右翼轮换，避免全挤一侧。
    /// </summary>
    public static void AssignSlots<T>(
        IList<T> combatants,
        Func<T, byte> typeId,
        Func<T, int> movement,
        Func<T, int> sortKey,
        Action<T, BattleFormationSlot> setSlot,
        bool isAttacker)
    {
        var ordered = combatants
            .OrderByDescending(movement)
            .ThenBy(sortKey)
            .ToList();

        var flankToggle = false;
        foreach (var c in ordered)
        {
            var preferred = PreferredSlot(typeId(c), isAttacker);
            if (typeId(c) == StrategyTroopTypes.Cavalry)
            {
                preferred = flankToggle ? BattleFormationSlot.FlankRight : BattleFormationSlot.FlankLeft;
                flankToggle = !flankToggle;
            }
            else if (typeId(c) == StrategyTroopTypes.Ashigaru && movement(c) < 5)
            {
                // 业务：低机动足轻改布中军，避免挤占前排
                preferred = BattleFormationSlot.Mid;
            }

            setSlot(c, preferred);
        }
    }

    /// <summary>阵位距离：同列 0，前后相邻 1，跨翼 2+。</summary>
    public static int SlotDistance(BattleFormationSlot a, BattleFormationSlot b)
    {
        if (a == b) return 0;

        static int Line(BattleFormationSlot s) => s switch
        {
            BattleFormationSlot.Front => 0,
            BattleFormationSlot.Mid => 1,
            BattleFormationSlot.Rear => 2,
            _ => 1
        };

        if (IsFlank(a) || IsFlank(b))
        {
            if (IsFlank(a) && IsFlank(b))
                return a == b ? 0 : 2;
            return 1 + Math.Abs(Line(IsFlank(a) ? b : a) - 1);
        }

        return Math.Abs(Line(a) - Line(b));
    }

    public static bool IsFlank(BattleFormationSlot slot)
        => slot is BattleFormationSlot.FlankLeft or BattleFormationSlot.FlankRight;

    /// <summary>是否为远程兵种（弓/铁炮）。</summary>
    public static bool IsRanged(byte typeId)
        => typeId is StrategyTroopTypes.Archer or StrategyTroopTypes.Matchlock;

    /// <summary>是否为骑兵兵种。</summary>
    public static bool IsCavalry(byte typeId)
        => typeId == StrategyTroopTypes.Cavalry;
}
