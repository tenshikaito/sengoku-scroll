using SengokuScroll.Common.Types;

namespace SengokuScroll.Domain.Entities;

/// <summary>地图级战场容器：野战或攻城；两侧单位列表；对峙日与主战记于此。</summary>
public class Battlefield
{
    public int Id { get; set; }

    public BattlefieldKind Kind { get; set; }

    /// <summary>交战格坐标。</summary>
    public Point2 Location { get; set; }

    /// <summary>关联战争；0 表示尚未绑定（应尽量避免）。</summary>
    public int WarId { get; set; }

    /// <summary>攻城目标据点；非攻城为 0。</summary>
    public int StrongholdId { get; set; }

    /// <summary>与 War 攻方同侧的单位 Id。</summary>
    public List<int> SideAUnitIds { get; set; } = [];

    /// <summary>与 War 守方同侧的单位 Id。</summary>
    public List<int> SideBUnitIds { get; set; } = [];

    /// <summary>侧 A 主战单位。</summary>
    public int MainCombatantAUnitId { get; set; }

    /// <summary>侧 B 主战单位。</summary>
    public int MainCombatantBUnitId { get; set; }

    public int StandoffDays { get; set; }

    public bool IsClosed { get; set; }
}

public enum BattlefieldKind : byte
{
    Field = 0,
    Siege = 1,
}
