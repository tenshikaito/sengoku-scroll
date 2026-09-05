using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Definitions;
using SengokuScroll.Domain.Entities.Abstraction;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Enums;
using SengokuScroll.Domain.Types;

namespace SengokuScroll.Domain.Entities;

/// <summary>
/// 角色
/// </summary>
public class Character : CharacterDefinition, IMovable
{
    /// <summary>
    /// 势力
    /// </summary>
    public int ForceId { get; set; }

    /// <summary>
    /// 所属据点
    /// </summary>
    public int StrongholdId { get; set; }

    /// <summary>
    /// 直属上司
    /// </summary>
    public int LeaderId { get; set; }

    /// <summary>
    /// 职位
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// 俸禄
    /// </summary>
    public int Salary { get; set; }

    /// <summary>个人金库储蓄（文）。</summary>
    public int Money { get; set; }

    /// <summary>
    /// 声望
    /// </summary>
    public int Popular { get; set; }

    /// <summary>进行中的募兵/征兵任务；无任务时为 null。</summary>
    public CharacterRecruitTask? RecruitTask { get; set; }

    /// <summary>进行中的外交使节任务；无任务时为 null。</summary>
    public CharacterDiplomacyMission? DiplomacyMission { get; set; }

    /// <summary>据点发布的募兵/征兵任务令；抵达目标后由 AI 转为执行任务。</summary>
    public CharacterRecruitAssignment? RecruitAssignment { get; set; }

    /// <summary>
    /// 恶名
    /// </summary>
    public int Notoriety { get; set; }

    /// <summary>
    /// 体力
    /// </summary>
    public int Hp { get; set; }

    /// <summary>
    /// 心情
    /// </summary>
    public int Emotion { get; set; }

    /// <summary>
    /// 生病
    /// </summary>
    public bool IsSick { get; set; }

    /// <summary>
    /// 行动力
    /// </summary>
    public int Ap { get; set; }

    /// <summary>
    /// 在地图上
    /// </summary>
    public CharacterLocationType LocationType { get; set; }

    /// <summary>
    /// 坐标
    /// </summary>
    public Point3 Location { get; set; }

    /// <summary>
    /// 所在据点
    /// </summary>
    public int LocationStrongholdId { get; set; }

    /// <summary>
    /// 所在设施
    /// </summary>
    public FacilityType FacilityType { get; set; }

    /// <summary>
    /// 在势力中的状态
    /// </summary>
    public CharacterForceStatus ForceStatus { get; set; }

    /// <summary>
    /// 行动计划
    /// </summary>
    public CharacterActionPlan ActionPlan { get; set; }

    /// <summary>
    /// 当前状态
    /// </summary>
    public CharacterActionStatus ActionStatus { get; set; }

    /// <summary>
    /// 行动目标
    /// </summary>
    public required CharacterActionTarget ActionTarget { get; set; }

    public bool IsUnit => false;

    public bool IsReadyToMove { get; set; }

    public bool IsMilitary => false;

    public Direction4 Direction { get; set; }

    public bool IsDead { get; set; }

    public  GameDate LastAiCheckDate { get; set; }

    /// <summary>父亲角色 Id；0 表示未知或无。</summary>
    public int FatherId { get; set; }

    /// <summary>母亲角色 Id；0 表示未知或无。</summary>
    public int MotherId { get; set; }

    /// <summary>配偶角色 Id；0 表示无。</summary>
    public int SpouseId { get; set; }

    /// <summary>师父角色 Id；0 表示无。</summary>
    public int MasterId { get; set; }

    /// <summary>仇敌角色 Id 列表。</summary>
    public List<int> EnemyIds { get; set; } = [];

    /// <summary>入仕日期；用于计算仕官年数（不直接展示）。</summary>
    public GameDate ServiceDate { get; set; }

    /// <summary>对势力的忠诚度 0–100；受 ActiveEffects 中 Loyalty 条目叠加。</summary>
    public byte Loyalty { get; set; } = 50;

    /// <summary>角色间关系（参照势力外交；含看法条目）。</summary>
    public List<CharacterRelationship> Relationships { get; set; } = [];
    public List<CharacterSocialMemory> SocialMemories { get; set; } = [];
    public List<CharacterDecisionRecord> RecentDecisions { get; set; } = [];
    public long NextSocialMemoryId { get; set; } = 1;
    public int? LastSocialAiDay { get; set; }
    public int? DefectionWarningDay { get; set; }
    public int PendingMarriageFromId { get; set; }
    public int? MarriageProposalExpiryDay { get; set; }

    /// <summary>当前增减益（灾害/事件/政策等）。</summary>
    public List<EntityEffect> ActiveEffects { get; set; } = [];

    /// <summary>当前任务列表（情报 · 任务 Tab）。</summary>
    public List<CharacterIntelTask> IntelTasks { get; set; } = [];

    public sealed class CharacterActionTarget : IActionTarget
    {
        public int ForceId { get; set; }

        public int StrongholdId { get; set; }

        public int UnitId { get; set; }

        public int CharacterId { get; set; }

        public required Queue<Point2> RoutePoints { get; set; }
    }

    public enum CharacterActionPlan : int
    {
        /// <summary>
        /// 休息
        /// </summary>
        /// <remarks>角色计划休息、会去寻找休息的地方例如回家或附近的旅店</remarks>
        Rest,
        /// <summary>
        /// 参加会议
        /// </summary>
        /// <remarks>角色计划参加会议、会去据点内的主家议事厅</remarks>
        Meet,
        /// <summary>
        /// 执行任务
        /// </summary>
        /// <remarks>角色计划执行任务、会优先去指定地点执行任务</remarks>
        Task,
        /// <summary>
        /// 报告任务结果
        /// </summary>
        /// <remarks>角色计划汇报任务结果、会优先去据点内的主家</remarks>
        Report,
    }

    /// <summary>
    /// 行动状态
    /// </summary>
    public enum CharacterActionStatus : int
    {
        /// <summary>
        /// 在当前地方待命
        /// </summary>
        Waiting,
        /// <summary>
        /// 休息中
        /// </summary>
        /// <remarks>角色体力会快速上升、视所在位置可能需要同时消耗金钱</remarks>
        Resting,
        /// <summary>
        /// 移动中
        /// </summary>
        /// <remarks>该状态表示正在按照指定的目标(据点或据点内设施)移动变化坐标</remarks>
        Moving,
        /// <summary>
        /// 行动中
        /// </summary>
        /// <remarks>正在做事情</remarks>
        Acting,
    }

    public enum CharacterForceStatus : int
    {
        /// <summary>
        /// 空闲
        /// </summary>
        /// <remarks>表示角色没有任何任务、可以自由活动</remarks>
        Idle,
        /// <summary>
        /// 任务中
        /// </summary>
        /// <remarks>表示角色有任务需要执行、需要优先执行任务</remarks>
        Task,
        /// <summary>
        /// 单位行动中
        /// </summary>
        /// <remarks>表示角色正在参与单位行动、无法执行个人指令</remarks>
        UnitAction,

        /// <summary>被敌方俘虏</summary>
        Prisoner,
    }

    public enum CharacterLocationType : int
    {
        /// <summary>在战略地图上移动。</summary>
        Map,

        /// <summary>驻留于据点内（设施/城内）。</summary>
        Stronghold,

        /// <summary>编入军事单位同行。</summary>
        Unit
    }
}
