namespace SengokuScroll.Domain.Entities;

/// <summary>两势力间的一对外交记录：关系值、信任、同盟/敌对状态与贡赋义务。</summary>
public class Diplomacy
{
    /// <summary>本方势力 Id。</summary>
    public int ForceId { get; set; }

    /// <summary>对方势力 Id。</summary>
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

    /// <summary>是否为从属势力（有宗主时不可独立宣战等）。</summary>
    public bool IsSubordinate => SuzerainId.HasValue;

    /// <summary>停战中；由系统按 <see cref="TrucePeriod"/> 自动更新。</summary>
    public bool IsTruce { get; set; }

    /// <summary>
    /// 停战期
    /// </summary>
    public ushort TrucePeriod { get; set; }

    /// <summary>贡赋义务：粮（产出比例，万分比）。</summary>
    public int TributeFoodBasisPoints { get; set; }

    /// <summary>贡赋义务：钱（产出比例，万分比）。</summary>
    public int TributeMoneyBasisPoints { get; set; }

    /// <summary>尚未运完的贡赋欠粮（合）。</summary>
    public int ArrearsFoodGo { get; set; }

    /// <summary>尚未运完的贡赋欠钱（文）。</summary>
    public int ArrearsMoney { get; set; }

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

    /// <summary>两势力当前外交立场（中立/同盟/敌对）。</summary>
    public enum DiplomacyRelation : byte
    {
        /// <summary>中立，可自由变更关系。</summary>
        Neutral = 0,

        /// <summary>同盟，不可互相攻击。</summary>
        Allied = 1,

        /// <summary>敌对，可交战并阻挡移动。</summary>
        Enemy = 2,
    }
}
