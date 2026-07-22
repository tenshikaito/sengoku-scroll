using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities.Abstraction;
using SengokuScroll.Domain.Entities.Types;

namespace SengokuScroll.Domain.Entities;

/// <summary>
/// 地图上的粮草运输队实体（系统自动派遣，玩家不直接操控）。
/// 与军事单位类似占据地图格；<see cref="IsMilitary"/> 恒为 false，可与友军同格，仅敌军队列阻挡（见 <see cref="Rules.MovementRules"/>）。
/// </summary>
public class SupplyConvoy : IHasForce, IMapObject, IHasLeader
{
    /// <summary>运输队唯一 Id。</summary>
    public int Id { get; set; }

    /// <summary>显示名（如「清洲粮运队」）。</summary>
    public required string Name { get; set; }

    /// <summary>所属势力 Id。</summary>
    public int ForceId { get; set; }

    /// <summary>运输队负责人（将领/奉行）角色 Id；0 表示未指派。</summary>
    public int LeaderId { get; set; }

    /// <summary>非军事单位；可与友军 occupy 同格。</summary>
    public bool IsMilitary => false;

    /// <summary>当前所在地图格。</summary>
    public Point3 Location { get; set; }

    /// <summary>出发据点 Id（粮秣出库处）。</summary>
    public int OriginStrongholdId { get; set; }

    /// <summary>补给目标军事单位 Id；为 0 时表示运往 <see cref="TargetStrongholdId"/>。</summary>
    public int TargetUnitId { get; set; }

    /// <summary>物资送达据点 Id（如当主居城）；<see cref="TargetUnitId"/> 为 0 时有效。</summary>
    public int TargetStrongholdId { get; set; }

    /// <summary>运载金钱（贯）；月初贡纳等。</summary>
    public int CargoMoney { get; set; }

    /// <summary>移民队运载人口数。</summary>
    public int CargoPopulation { get; set; }

    /// <summary>运输任务类型。</summary>
    public TransportPurpose Purpose { get; set; } = TransportPurpose.Supply;

    /// <summary>当前载粮量（合）。在途每日由人夫/护卫消耗从此扣除。</summary>
    public int CargoFoodGo { get; set; }

    /// <summary>人夫数量，影响在途自耗粮。</summary>
    public int PorterCount { get; set; }

    /// <summary>护卫兵数量，影响在途自耗粮与遇敌战力（M3 起）。</summary>
    public int EscortSoldierCount { get; set; }

    /// <summary>运输队生命周期状态。</summary>
    public SupplyConvoyStatus Status { get; set; }

    /// <summary>剩余路径格点队列；队首为下一日目标格。</summary>
    public Queue<Point3> RoutePoints { get; set; } = new();

    /// <summary>是否正被假情报迷惑（信使 <see cref="MessagePayloadType.FalseIntelligence"/> 所致）。</summary>
    public bool IsDeceived { get; set; }

    /// <summary>假情报误导的目标格；迷惑解除后可能改道至此（M3 完善）。</summary>
    public Point3? DeceivedRedirect { get; set; }

    /// <summary>迷惑状态下剩余停留天数；大于 0 时不沿原路径移动。</summary>
    public int DeceivedHoldDaysRemaining { get; set; }

    /// <summary>是否处于卸粮后返回出发据点的返程阶段（返程中无载粮，不触发粮尽销毁）。</summary>
    public bool IsReturningToOrigin { get; set; }

    /// <summary>每日移动力上限。</summary>
    public int Movement { get; set; }

    /// <summary>当日剩余移动力；新建时为 0，日初恢复后再移动。</summary>
    public int Ap { get; set; }
}
