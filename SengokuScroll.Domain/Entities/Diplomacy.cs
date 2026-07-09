namespace SengokuScroll.Domain.Entities;

public class Diplomacy
{
    public int ForceId { get; set; }

    public int TargetForceId { get; set; }

    /// <summary>
    /// 外交关系
    /// </summary>
    /// <remarks>外交关系越高达成同盟或王室联姻的关系的成功率就越高、宣战时进攻方的惩罚也就越高</remarks>
    /// <value>-100=关系恶劣</value>
    /// <value>100=关系亲密</value>
    public sbyte Relationship { get; set; }

    /// <summary>
    /// 信任度
    /// </summary>
    /// <remarks>信任度影响对对方的信任水平、以至于影响同盟、联姻、战争支援等的可行性</remarks>
    /// <value>-100=毫不信任</value>
    /// <value>100=完全信任</value>
    public sbyte Trust { get; set; }

    /// <summary>
    /// 外交策略
    /// </summary>
    public DiplomacyStrategy Strategy { get; set; }

    /// <summary>
    /// 外交状态
    /// </summary>
    public DiplomacyRelation Relation { get; set; }

    /// <summary>
    /// 宗主势力
    /// </summary>
    public int? SuzerainId { get; set; }

    /// <summary>
    /// 是从属势力
    /// </summary>
    public bool IsSubordinate => SuzerainId.HasValue;

    /// <summary>
    /// 停战中
    /// 由系统根据时长更新状态123
    /// </summary>
    public bool IsTruce { get; set; }

    /// <summary>
    /// 停战期
    /// </summary>
    public ushort TrucePeriod { get; set; }

    public enum DiplomacyStrategy : byte
    {
        /// <summary>
        /// 维持现状
        /// </summary>
        Maintain,
        /// <summary>
        /// 友好
        /// </summary>
        /// <remarks>会持续考虑增进与对方的关系</remarks>
        Friend,
        /// <summary>
        /// 敌视
        /// </summary>
        /// <remarks>会持续考虑破坏与对方的关系</remarks>
        Enemy,
        /// <summary>
        /// 同盟
        /// </summary>
        /// <remarks>会持续考虑增进与对方的关系直到同盟达成</remarks>
        Ally,
        /// <summary>
        /// 联姻
        /// </summary>
        /// <remarks>会持续考虑增进与对方的关系直到联姻达成</remarks>
        Marriage,
        /// <summary>
        /// 支配
        /// </summary>
        /// <remarks>会持续考虑改变对方的关系直到能够支配对方</remarks>
        Control,
        /// <summary>
        /// 从属
        /// </summary>
        /// <remarks>会持续考虑增进对方的关系直到能够从属对方</remarks>
        Submit,
    }

    public enum DiplomacyRelation : byte
    {
        Neutral = 0,
        Allied = 1,
        Enemy = 2,
    }
}
