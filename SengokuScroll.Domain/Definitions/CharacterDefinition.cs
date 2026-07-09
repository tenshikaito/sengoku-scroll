using SengokuScroll.Common.Models;
using SengokuScroll.Domain.Types;

namespace SengokuScroll.Domain.Definitions;

/// <summary>
/// 角色
/// </summary>
public class CharacterDefinition : GameModelBase
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    public required string Portrait { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    public SexType Sex { get; set; }

    /// <summary>
    /// 出生年
    /// </summary>
    public GameDate Birthday { get; set; }

    /// <summary>
    /// 类型
    /// </summary>
    public CharacterType Type { get; set; }

    /// <summary>
    /// 出身
    /// </summary>
    public BirtyType Birth { get; set; }

    /// <summary>
    /// 文化
    /// </summary>
    public int CultureId { get; set; }

    /// <summary>
    /// 信仰(国教)
    /// </summary>
    public int RegligionId { get; set; }

    /// <summary>
    /// 属性
    /// </summary>
    public required PersonalityData Personality { get; set; }

    /// <summary>
    /// 武器
    /// </summary>
    public int WeaponId { get; set; }

    /// <summary>
    /// 防具
    /// </summary>
    public int ArmorId { get; set; }

    /// <summary>
    /// 载具
    /// </summary>
    public int AmountId { get; set; }

    /// <summary>
    /// 统率
    /// </summary>
    /// <remarks></remarks>
    public byte Leadership { get; set; }

    /// <summary>
    /// 力量
    /// </summary>
    public byte Power { get; set; }

    /// <summary>
    /// 政治
    /// </summary>
    public byte Politics { get; set; }

    /// <summary>
    /// 智谋
    /// </summary>
    public byte Strategy { get; set; }

    /// <summary>
    /// 魅力
    /// </summary>
    public byte Charm { get; set; }

    /// <summary>
    /// 能力熟练度
    /// </summary>
    public required ProficiencyData Proficiency { get; set; }

    public enum SexType : byte
    {
        Male = 0,
        Female = 1
    }

    public enum CharacterType : byte
    {
        AI = 0,
        Player = 1
    }

    public enum BirtyType : sbyte
    {
        /// <summary>
        /// 奴隶
        /// </summary>
        Slave = -1,

        /// <summary>
        /// 平民
        /// </summary>
        Normal = 0,

        /// <summary>
        /// 豪强士族
        /// </summary>
        Landlord = 1,

        /// <summary>
        /// 贵族
        /// </summary>
        Noble = 2,

        /// <summary>
        /// 皇族
        /// </summary>
        RoyalFamily = 3
    }

    public sealed class PersonalityData
    {
        /// <summary>
        /// 性情: 0=急躁, 100=温和
        /// </summary>
        public byte Temper { get; set; }

        /// <summary>
        /// 勇气: 0=胆小, 100=钢胆
        /// </summary>
        public byte Courage { get; set; }

        /// <summary>
        /// 主义: 0=现实, 100=理想
        /// </summary>
        public byte Principle { get; set; }

        /// <summary>
        /// 行动: 0=轻率, 100=慎重
        /// </summary>
        public byte Action { get; set; }

        /// <summary>
        /// 情义: 0=轻视, 100=重视
        /// </summary>
        public byte Friendship { get; set; }

        /// <summary>
        /// 野心
        /// </summary>
        public byte Ambition { get; set; }

        /// <summary>
        /// 喜好
        /// </summary>
        public byte Hobby { get; set; }

        /// <summary>
        /// 物欲
        /// </summary>
        public byte Desire { get; set; }

        /// <summary>
        /// 饮酒
        /// </summary>
        public byte Drinking { get; set; }

        /// <summary>
        /// 运势
        /// </summary>
        public byte Fortune { get; set; }
    }

    public sealed class ProficiencyData
    {
        /// <summary>
        /// 步兵
        /// </summary>
        public required ProficiencyStats Infantry { get; set; }

        /// <summary>
        /// 骑马
        /// </summary>
        public required ProficiencyStats Ride { get; set; }

        /// <summary>
        /// 弓术
        /// </summary>
        public required ProficiencyStats Archery { get; set; }

        /// <summary>
        /// 火枪
        /// </summary>
        public required ProficiencyStats Firelock { get; set; }

        /// <summary>
        /// 航海
        /// </summary>
        public required ProficiencyStats Sealing { get; set; }

        /// <summary>
        /// 军略
        /// </summary>
        public required ProficiencyStats Military { get; set; }

        /// <summary>
        /// 战斗
        /// </summary>
        public required ProficiencyStats Fighting { get; set; }

        /// <summary>
        /// 谍报
        /// </summary>
        public required ProficiencyStats Spy { get; set; }

        /// <summary>
        /// 农业
        /// </summary>
        public required ProficiencyStats Agriculture { get; set; }

        /// <summary>
        /// 商业
        /// </summary>
        public required ProficiencyStats Commerce { get; set; }

        /// <summary>
        /// 建筑
        /// </summary>
        public required ProficiencyStats Construct { get; set; }

        /// <summary>
        /// 冶炼
        /// </summary>
        public required ProficiencyStats Smelt { get; set; }

        /// <summary>
        /// 辩才
        /// </summary>
        public required ProficiencyStats Eloquence { get; set; }

        /// <summary>
        /// 宫廷
        /// </summary>
        public required ProficiencyStats Court { get; set; }

        /// <summary>
        /// 交际
        /// </summary>
        public required ProficiencyStats Sociality { get; set; }

        /// <summary>
        /// 医术
        /// </summary>
        public required ProficiencyStats Healing { get; set; }
    }

    public sealed class ProficiencyStats
    {
        public byte Level;

        public byte Exp;

        public static implicit operator ProficiencyStats(byte level) => new() { Level = level };
    }
}
