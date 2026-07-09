namespace SengokuScroll.Domain.Entities;

public class Force : StrongholdActor
{
    /// <summary>
    /// 政体
    /// </summary>
    public int PoliticalSystemId { get; set; }

    /// <summary>
    /// 稳定度
    /// </summary>
    public byte Stability { get; set; }

    /// <summary>
    /// 声望
    /// </summary>
    public byte Prestige { get; set; }

    /// <summary>
    /// 合法性
    /// </summary>
    public byte Orthodoxy { get; set; }

    /// <summary>
    /// 继承人
    /// </summary>
    public int? Successor { get; set; }

    /// <summary>
    /// 可相容文化
    /// </summary>
    public required List<int> AcceptedCultureIds { get; set; }

    /// <summary>
    /// 省份
    /// </summary>
    /// <remarks>可以对占领的据点编排省份、被编入省份的据点可以认为是取得核心、若失去控制则可以作为宣战理由</remarks>
    public required List<Province> Provinces { get; set; }

    /// <summary>
    /// 势力身份
    /// </summary>
    public ForceStatus Status { get; set; }

    /// <summary>
    /// 宗主国
    /// </summary>
    public int? SuzerainForceId { get; set; }

    /// <summary>
    /// 势力战略
    /// </summary>
    public ForceStrategy Strategy { get; set; }

    /// <summary>
    /// 势力战术
    /// </summary>
    public ForceTactics Tactics { get; set; }

    /// <summary>
    /// 外交
    /// </summary>
    public required List<Diplomacy> Diplomacies { get; set; }

    public enum ForceStatus : byte
    {
        /// <summary>
        /// 独立国家
        /// </summary>
        Independence,
        /// <summary>
        /// 外藩
        /// </summary>
        /// <remarks>表面上对我方臣服的势力、有独立外交权和军事权、没有控制权</remarks>
        OuterVassal,
        /// <summary>
        /// 内藩
        /// </summary>
        /// <remarks>实际上属于我方的势力、没有独立外交权和军事权、可控制属国的一切甚至撤藩</remarks>
        InnerVassal,
    }

    public enum ForceStrategy : byte
    {
        /// <summary>
        /// 守备领土
        /// </summary>
        Hold,
        /// <summary>
        /// 地区制霸
        /// </summary>
        Area,
        /// <summary>
        /// 天下制霸
        /// </summary>
        World,
    }

    public enum ForceTactics : byte
    {
        /// <summary>
        /// 战争为主
        /// </summary>
        War,
        /// <summary>
        /// 外交为主
        /// </summary>
        Diplomacy,
    }
}
