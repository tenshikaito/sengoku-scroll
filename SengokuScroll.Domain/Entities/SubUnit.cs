namespace SengokuScroll.Domain.Entities;

public class SubUnit
{
    public int Id { get; set; }

    public byte TypeId { get; set; }

    /// <summary>兵种显示名（如足轻、弓兵）；空则按 TypeId 推断。</summary>
    public string TypeName { get; set; } = "";

    /// <summary>编制队名（UI 显示时加「队」；如「足轻一」→「足轻一队」）。</summary>
    public string UnitName { get; set; } = "";

    /// <summary>该队士气；0 表示沿用据点 ForceActor 均值。</summary>
    public byte Morale { get; set; }

    /// <summary>该队训练度；0 表示沿用据点 ForceActor 均值。</summary>
    public byte Training { get; set; }

    /// <summary>是否骑马（与兵种独立；弓兵/铁炮也可为骑射/骑铁炮）。</summary>
    public bool IsMounted { get; set; }

    public int ForceId { get; set; }

    public int StrongholdId { get; set; }

    public int UnitId { get; set; }

    /// <summary>该编制段兵数（备队/兵种小队规模）。</summary>
    public int Soldier { get; set; }

    public int LeaderId { get; set; }

    public int Attack { get; set; }

    public int Defense { get; set; }

    public int AttackRange { get; set; }

    public int Movement { get; set; }

    public byte Tiredness { get; set; }

    /// <summary>
    /// 航行中
    /// </summary>
    public bool IsSealing { get; set; }
}
