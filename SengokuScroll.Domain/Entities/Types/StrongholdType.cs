using SengokuScroll.Domain.Enums;

namespace SengokuScroll.Domain.Entities.Types;

/// <summary>
/// 据点定义
/// </summary>
public class StrongholdType
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    /// <summary>
    /// 文化特有类型
    /// </summary>
    public int? CultureId { get; set; }

    public CategoryType Category { get; set; }

    public int NecessaryGuardianNumber { get; set; }

    public int Cost { get; set; }

    public int Maintenance { get; set; }

    public enum CategoryType
    {
        /// <summary>
        /// 平城
        /// </summary>
        /// <remarks>
        /// <para>只能建在平地上</para>
        /// <para>在水边将会自带港口可以建造船只</para>
        /// <para>也可能被水攻</para>
        /// </remarks>
        Plain = 0,
        /// <summary>
        /// 平山城
        /// </summary>
        /// <remarks>只能建在丘陵上</remarks>
        Hill = 1,
        /// <summary>
        /// 山城
        /// </summary>
        /// <remarks>只能建在山地上</remarks>
        Mountain = 2,
    }
}

public class DefenseFacilityTypeModel
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public DefenseFacilityCategory Category { get; set; }

    public Level3 Level { get; set; }

    public int Cost { get; set; }

    public int Maintenance { get; set; }

    public int Attack { get; set; }

    public int Defense { get; set; }

    public int Movement { get; set; }

    public enum DefenseFacilityCategory : byte
    {
        /// <summary>
        /// 城堡
        /// </summary>
        Castle = 0,
        /// <summary>
        /// 城墙
        /// </summary>
        Wall = 1,
        /// <summary>
        /// 城门
        /// </summary>
        Gate = 2,
        /// <summary>
        /// 护城河
        /// </summary>
        Moat = 3,
        /// <summary>
        /// 防御设施
        /// </summary>
        Defender = 4,
    }
}
