using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities.Abstraction;
using SengokuScroll.Domain.Enums;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Domain.Entities;

public class Unit : Actor, IUnit
{
    public Point3 Location { get; set; }

    public int Ap { get; set; }

    /// <summary>
    /// 阵型
    /// </summary>
    public int FormationId { get; set; }

    public int Attack { get; set; }

    public int Defense { get; set; }

    public int AttackRange { get; set; }

    public int Movement { get; set; }

    public int Tiredness { get; set; }

    /// <summary>
    /// 航行中
    /// </summary>
    public bool IsSealing { get; set; }

    /// <summary>
    /// 方针
    /// </summary>
    public UnitDirective Directive { get; set; }

    public int DirectiveTargetId { get; set; }

    /// <summary>
    /// 姿态
    /// </summary>
    /// <remarks>影响移动、防御、疲劳和伏兵发现概率</remarks>
    public UnitStance Stance { get; set; }

    public UnitStatus Status { get; set; }

    public bool IsUnit => true;

    public bool IsReadyToMove { get; set; }

    public bool IsMilitary { get; set; }

    public Direction4 Direction { get; set; }

    /// <summary>
    /// 行动目标
    /// </summary>
    public required UnitActionTarget ActionTarget { get; set; }

    public sealed class UnitActionTarget : IActionTarget
    {
        public int ForceId { get; set; }

        public int StrongholdId { get; set; }

        public int UnitId { get; set; }

        public int CharacterId { get; set; }

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
    }
}
