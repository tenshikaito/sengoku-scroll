using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities.Abstraction;
using SengokuScroll.Domain.Enums;
using SengokuScroll.Domain.Entities.Types;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Domain.Entities;

/// <summary>地图上的军事单位：野战/攻城部队，含战斗属性、方针与姿态。</summary>
public class Unit : Actor, IUnit
{
    /// <summary>当前所在地图格坐标。</summary>
    public Point3 Location { get; set; }

    /// <summary>当日剩余行动力；移动与攻击均消耗。</summary>
    public int Ap { get; set; }

    /// <summary>
    /// 阵型
    /// </summary>
    public int FormationId { get; set; }

    /// <summary>攻击力。</summary>
    public int Attack { get; set; }

    /// <summary>防御力。</summary>
    public int Defense { get; set; }

    /// <summary>攻击射程（格数）。</summary>
    public int AttackRange { get; set; }

    /// <summary>每日移动力上限。</summary>
    public int Movement { get; set; }

    /// <summary>疲劳度；影响战斗与恢复。</summary>
    public int Tiredness { get; set; }

    /// <summary>
    /// 航行中
    /// </summary>
    public bool IsSealing { get; set; }

    /// <summary>
    /// 方针
    /// </summary>
    public UnitDirective Directive { get; set; }

    /// <summary>方针关联目标 Id（据点/单位/角色，依方针类型而定）。</summary>
    public int DirectiveTargetId { get; set; }

    /// <summary>
    /// 攻城方式（下达攻城指令后生效；无则 None）。
    /// </summary>
    public UnitSiegeMode SiegeMode { get; set; }

    /// <summary>
    /// 姿态
    /// </summary>
    /// <remarks>影响移动、防御、疲劳和伏兵发现概率</remarks>
    public UnitStance Stance { get; set; }

    /// <summary>单位生命周期状态（待机、移动、对峙、埋伏等）。</summary>
    public UnitStatus Status { get; set; }

    /// <summary>所属地图战场容器 Id；0 表示未入战场。</summary>
    public int BattlefieldId { get; set; }

    /// <summary>进入当前战场前所在邻格（溃逃原路）；未记录则为 null。</summary>
    public Point2? BattlefieldEntryFrom { get; set; }

    /// <summary>溃逃剩余日数（Routing 时）。</summary>
    public int RoutingDaysRemaining { get; set; }

    public bool IsUnit => true;

    /// <summary>日初是否已恢复行动力、可接受移动指令。</summary>
    public bool IsReadyToMove { get; set; }

    /// <summary>是否为军事单位（恒为 true；用于与同格非军事实体区分）。</summary>
    public bool IsMilitary { get; set; }

    /// <summary>单位种类（军事/运输/贸易/移民等）。</summary>
    public UnitKind Kind { get; set; } = UnitKind.Military;

    /// <summary>是否在城内待命；为 true 时不占 <see cref="GameMapData.Units"/> 格索引。</summary>
    public bool InStronghold { get; set; }

    /// <summary>编制归属据点；仅在此城可建制解散。</summary>
    public int HomeStrongholdId { get; set; }

    /// <summary>当前驻留据点（协防友城时可与 Home 不同）。</summary>
    public int LocationStrongholdId { get; set; }

    /// <summary>连续断粮日数（携行粮为 0 且补给 CutOff 时递增）。</summary>
    public int SupplyCollapseDays { get; set; }

    /// <summary>贸易任务策略（非 UnitDirective）。</summary>
    public UnitTradePolicy TradePolicy { get; set; }

    /// <summary>贸易限价（文/合）；WaitBuy 为最高可接受买价，WaitSell 为最低可接受卖价。</summary>
    public int TradeLimitPriceMoneyPerGo { get; set; }

    /// <summary>贸易目标数量（合）；0 表示尽可能成交。</summary>
    public int TradeQuantityGo { get; set; }

    public Direction4 Direction { get; set; }

    /// <summary>当前行动目标：势力/据点/单位/角色 Id 及剩余路径。</summary>
    public required UnitActionTarget ActionTarget { get; set; }

    /// <summary>单位行动目标的各维度 Id 与路径队列。</summary>
    public sealed class UnitActionTarget : IActionTarget
    {
        /// <summary>目标势力 Id。</summary>
        public int ForceId { get; set; }

        /// <summary>目标据点 Id。</summary>
        public int StrongholdId { get; set; }

        /// <summary>目标军事单位 Id。</summary>
        public int UnitId { get; set; }

        /// <summary>目标角色 Id。</summary>
        public int CharacterId { get; set; }

        /// <summary>剩余路径格点队列；队首为下一日目标格。</summary>
        public required Queue<Point2> RoutePoints { get; set; }
    }

    public enum UnitDirective : int
    {
        /// <summary>移动</summary>
        Move,

        /// <summary>占领</summary>
        Occupy,

        /// <summary>劫掠</summary>
        Raid,

        /// <summary>支援</summary>
        Support,

        /// <summary>撤退</summary>
        Retreat,
    }

    /// <summary>攻城指令分支：强攻（接敌决战）或包围（封锁不主动进攻）。</summary>
    public enum UnitSiegeMode : int
    {
        None = 0,
        /// <summary>包围：维持封锁，每日消耗守方士气/粮储，不主动接敌。</summary>
        Encircle,
        /// <summary>强攻：对据点驻军发起接敌，空城则在据点上宣告占领。</summary>
        Assault,
    }

    public enum UnitStance : int
    {
        /// <summary>普通状态</summary>
        Normal,

        /// <summary>攻击中</summary>
        /// <remarks>下一回合会自动继续攻击目标</remarks>
        Attacking,

        /// <summary>包围中</summary>
        /// <remarks>进入包围状态后被攻击方无法移动、且士气下降度提高直到包围状态解除</remarks>
        Surrounding,

        /// <summary>机动，移动上升，发现伏兵概率下降</summary>
        Maneuver,

        /// <summary>警惕，移动下降，发现伏兵概率上升</summary>
        Alert,

        /// <summary>坚守，无法移动，防御上升，疲劳度增加度降低</summary>
        Hold
    }

    public enum UnitStatus : int
    {
        /// <summary>原地待机</summary>
        /// <remarks>
        /// <para>如果是普通状态、防御下降疲劳下降</para>
        /// <para>如果是警惕状态、防御略微下降、疲劳略微下降</para>
        /// </remarks>
        Waiting = 0,
        /// <summary>移动中</summary>
        /// <remarks>
        /// <para>如果是普通状态、一切正常</para>
        /// <para>如果是机动状态、移动力上升、发现伏兵概率下降</para>
        /// <para>如果是警惕状态、移动力下降、发现伏兵概率上升</para>
        /// </remarks>
        Moving,
        /// <summary>斗志高昂</summary>
        /// <remarks>士气上升攻击力上升</remarks>
        Inspiring,
        /// <summary>恐惧</summary>
        /// <remarks>士气下降攻击力下降防御力下降、姿态可能变为机动</remarks>
        Fearful,
        /// <summary>混乱</summary>
        /// <remarks>士兵数缓慢降低、攻击力极大降低防御力极大降低无法移动</remarks>
        Chaos,
        /// <summary>埋伏</summary>
        /// <remarks>地图上不可见、也不能移动、有几率被发现、如果遇到敌军进入格子可选择攻击</remarks>
        Ambushing,
        /// <summary>被包围中</summary>
        /// <remarks>进入包围状态后无法移动、且士气下降度提高直到包围状态解除</remarks>
        BeingSurround,

        /// <summary>战场对峙</summary>
        /// <remarks>与敌军同格接敌后列阵对峙；保持至一方强袭、撤退或离开战场</remarks>
        Standoff,

        /// <summary>溃逃</summary>
        /// <remarks>战败未全灭后强制撤离；优先沿入场方向；再接敌易溃散</remarks>
        Routing,
    }
}
